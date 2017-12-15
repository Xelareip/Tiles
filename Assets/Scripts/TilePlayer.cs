﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class TilePlayer : MonoBehaviour
{
	public static TilePlayer Instance { get; private set; }

	public GameObject endGame;
	public CameraMove cameraMove;

	public float speed;

	public TileBase currentTile;
	public List<TileBase> tilesQueue = new List<TileBase>();
	public int inputQueueSize;

	public LineRenderer pathLine;

	public List<TileBase> clickableTiles = new List<TileBase>();

	public List<Transform> loopGhosts;

	public int maxDistance;

	public float difficulty;

	public float autoMoveTimer;
	public GameObject automoveVisual;
	public TileBase autoMoveTile;

	private bool TileReached()
	{
		if (tilesQueue.Count > 0)
		{
			// ReSharper disable CompareOfFloatsByEqualityOperator
			return transform.position.x == tilesQueue[0].transform.position.x && transform.position.y == tilesQueue[0].transform.position.y;
			// ReSharper restore CompareOfFloatsByEqualityOperator
		}
		return true;
    }

	private void Awake()
	{
		loopGhosts[0].transform.position = transform.position + Vector3.left * TileManager.Instance.GetWidth();
		loopGhosts[1].transform.position = transform.position + Vector3.right * TileManager.Instance.GetWidth();
		Instance = this;
		speed = Parameters.Instance.playerSpeed;
		if (Parameters.Instance.inputQueueSize < 0)
		{
			inputQueueSize = int.MaxValue;
		}
		else
		{
			inputQueueSize = Parameters.Instance.inputQueueSize;
		}
		maxDistance = 0;
		difficulty = 0;
		autoMoveTimer = Parameters.Instance.autoMoveDelay;
		if (Parameters.Instance.autoMove)
		{
			cameraMove.followPlayer = true;
		}
	}

	private void Start()
	{
		SwipeManager.Instance.Swiped += OnSwipe;
	}

	private void OnDestroy()
	{
		SwipeManager.Instance.Swiped -= OnSwipe;
	}

	public float GetSpeed()
	{
		return speed * (difficulty * Parameters.Instance.difficultyIncrease / 100.0f + 1.0f);
	}

	public float GetCameraSpeed()
	{
		return Parameters.Instance.cameraSpeed * (difficulty * Parameters.Instance.difficultyIncrease / 100.0f + 1.0f);
	}

	public TileBase GetRootTile()
	{
		if (tilesQueue.Count > 0)
		{
			return tilesQueue[tilesQueue.Count - 1];
		}
		else
		{
			return currentTile;
		}
	}

	public void OnSwipe(float angle)
	{
		if (Parameters.Instance.swipeControl)
		{
			TileBase rootTile = GetRootTile();
			float bestDot = float.MinValue;
			TileBase bestTarget = null;
			for (int targetIdx = 0; targetIdx < clickableTiles.Count; ++targetIdx)
			{
				TileBase potentialTarget = clickableTiles[targetIdx];
				if (potentialTarget == null)
				{
					continue;
				}

				float neighborAngle = rootTile.NeighborToAngle(potentialTarget);
				float dot = Vector3.Dot(Quaternion.AngleAxis(neighborAngle, Vector3.forward) * Vector3.up, Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up);

				if (dot > bestDot)
				{
					bestDot = dot;
					bestTarget = potentialTarget;
				}
			}

			if (bestTarget != null)
			{
				QueueTile(bestTarget);
			}
		}
	}

	public void MoveLeft()
	{
		TileBase rootTile;
		if (tilesQueue.Count > inputQueueSize - 1)
		{
			return;
		}
		else if (tilesQueue.Count > 0)
		{
			rootTile = tilesQueue[tilesQueue.Count - 1];
		}
		else
		{
			rootTile = currentTile;
		}
		if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.NORTH_WEST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.NORTH_WEST]);
		}
		else if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.WEST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.WEST]);
		}
		else if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.SOUTH_WEST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.SOUTH_WEST]);
		}
	}

	public void MoveNorth()
	{
		TileBase rootTile;
		if (tilesQueue.Count > inputQueueSize - 1)
		{
			return;
		}
		else if (tilesQueue.Count > 0)
		{
			rootTile = tilesQueue[tilesQueue.Count - 1];
		}
		else
		{
			rootTile = currentTile;
		}
		if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.NORTH]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.NORTH]);
		}
		else if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.SOUTH]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.SOUTH]);
		}
	}

	public void MoveRight()
	{
		TileBase rootTile;
		if (tilesQueue.Count > inputQueueSize - 1)
		{
			return;
		}
		else if (tilesQueue.Count > 0)
		{
			rootTile = tilesQueue[tilesQueue.Count - 1];
		}
		else
		{
			rootTile = currentTile;
		}
		if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.NORTH_EAST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.NORTH_EAST]);
		}
		else if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.EAST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.EAST]);
		}
		else if (clickableTiles.Contains(rootTile.neighbors[(int)DIRECTIONS.SOUTH_EAST]))
		{
			QueueTile(rootTile.neighbors[(int)DIRECTIONS.SOUTH_EAST]);
		}
	}

	public TileBase MoveDirection(string dir, bool force = false)
	{
		DIRECTIONS direction = (DIRECTIONS)Enum.Parse(typeof(DIRECTIONS), dir);
		TileBase rootTile = GetRootTile();
		if (rootTile == false)
		{
			return null;
		}
		TileBase target = rootTile.neighbors[(int)direction];
        if (target != null)
		{
			if (force || clickableTiles.Contains(target))
			{
				QueueTile(target);
				return target;
			}
		}
		return null;
	}

	public void FindTile()
	{
		float minDist = float.MaxValue;
		TileBase closestTile = null;
		for (int lineIdx = 0; lineIdx < TileManager.Instance.tileLines.Count; ++lineIdx)
		{
			TileLine currentLine = TileManager.Instance.tileLines[lineIdx];
			for (int tileIdx = 0; tileIdx < currentLine.tiles.Count; ++tileIdx)
			{
				TileBase loopTile = currentLine.tiles[tileIdx];

				float dist = Vector3.Distance(loopTile.transform.position, transform.position);
				if (dist < minDist)
				{
					minDist = dist;
					closestTile = loopTile;
				}
			}
		}

		currentTile = closestTile;
	}

	private void CheckKeyboard()
	{
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			MoveLeft();
		}
		if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
		{
			ForceAutoMove();
		}
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			MoveRight();
		}
	}

	private void Update ()
	{
		if (CheckDeath())
		{
			EndGame();
			return;
		}
		AutoMove();
		UpdateAutoMoveVisual();
		cameraMove.speed = Vector3.up * GetCameraSpeed();
		difficulty = maxDistance;
		CheckKeyboard();
		if (GoToTile())
		{
			int lineNumber = tilesQueue[0].parentLine.lineNumber;
            if (Parameters.Instance.pointsPerLine && lineNumber > maxDistance)
			{
				ScoreManager.Instance.score += lineNumber - maxDistance;
				maxDistance = lineNumber;
            }

			if (autoMoveTile == tilesQueue[0])
			{
				autoMoveTile = null;
				autoMoveTimer = Parameters.Instance.autoMoveDelay;
			}
			tilesQueue[0].TileReached();
			currentTile = tilesQueue[0];
			if (tilesQueue.Count > 0)
			{
				tilesQueue.RemoveAt(0);
			}
			if (tilesQueue.Count > 0)
			{
				currentTile.TileLeft();
			}
			FindBestGhost();
		}

		ActivateClickableTiles();
		DrawPath();
	}


	public void AutoMove()
	{
		if (Parameters.Instance.autoMove)
		{
			bool timerPassed = autoMoveTimer < 0.0f;
			autoMoveTimer -= Time.deltaTime;

			if (timerPassed == false && autoMoveTimer < 0.0f)
			{
				Debug.Log("Moving forward");
				TileBase savedTile = null;
				if (tilesQueue.Count > 0)
				{
					savedTile = tilesQueue[0];
				}
				tilesQueue.Clear();
				QueueTile(savedTile);
				autoMoveTile = MoveDirection("NORTH", true);
			}
		}
	}

	public void UpdateAutoMoveVisual()
	{
		float angle = 360.0f / Parameters.Instance.autoMoveDelay * Mathf.Max(autoMoveTimer, 0.0f);

		automoveVisual.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
	}

	public void ForceAutoMove()
	{
		autoMoveTimer = 0.0f;
	}

	public void FindBestGhost()
	{
		if (tilesQueue.Count > 0)
		{
			float bestDist = Vector3.Distance(transform.position, tilesQueue[0].transform.position);
			Vector3 bestPos = transform.position;
			for (int ghostId = 0; ghostId < loopGhosts.Count; ++ghostId)
			{
				float dist = Vector3.Distance(loopGhosts[ghostId].transform.position, tilesQueue[0].transform.position);

				if (dist < bestDist)
				{
					bestDist = dist;
					bestPos = loopGhosts[ghostId].transform.position;
				}
			}

			transform.position = bestPos;
		}
	}

	public void DrawPath()
	{
		if (Parameters.Instance.drawPath == false)
		{
			return;
		}
		if (tilesQueue.Count > 0)
		{
			pathLine.enabled = true;
			Vector3[] pathPoints = new Vector3[tilesQueue.Count + 1];
			pathPoints[0] = transform.position;

			for (int i = 0; i < tilesQueue.Count; ++i)
			{
				pathPoints[i + 1] = tilesQueue[i].transform.position;
			}
			pathLine.positionCount = tilesQueue.Count + 1;
			pathLine.SetPositions(pathPoints);
		}
		else
		{
			pathLine.enabled = false;
		}
	}

	public void EndGame()
	{
		endGame.SetActive(true);
		Destroy(gameObject);
	}

	private bool CheckDeath()
	{
		if (Parameters.Instance.autoMove)
		{
			return false;
		}
		else
		{
			return transform.position.y < TileManager.Instance.KillHeight();
		}
	}

	private bool AllowedDirection(DIRECTIONS dir)
	{
		switch(dir)
		{
			case DIRECTIONS.NORTH:
				return Parameters.Instance.moveNorth;
			case DIRECTIONS.SOUTH:
				return Parameters.Instance.moveSouth;
			case DIRECTIONS.EAST:
				return Parameters.Instance.moveEast;
			case DIRECTIONS.WEST:
				return Parameters.Instance.moveWest;
			case DIRECTIONS.NORTH_EAST:
				return Parameters.Instance.moveNorthEast;
			case DIRECTIONS.NORTH_WEST:
				return Parameters.Instance.moveNorthWest;
			case DIRECTIONS.SOUTH_EAST:
				return Parameters.Instance.moveSouthEast;
			case DIRECTIONS.SOUTH_WEST:
				return Parameters.Instance.moveSouthWest;
		}
		return false;
	}

	public void QueueTile(TileBase tile)
	{
		if (tile == null)
		{
			return;
		}
		if (tilesQueue.Count < inputQueueSize)
		{
			tilesQueue.Add(tile);
			if (tilesQueue.Count == 1)
			{
				FindBestGhost();
			}
			if (tilesQueue.Count == 1)
			{
				currentTile.TileLeft();
			}
		}
	}

	private void ActivateClickableTiles()
	{
		clickableTiles.Clear();

		TileBase rootTile;
		if (tilesQueue.Count > inputQueueSize - 1)
		{
			return;
		}
		else if (tilesQueue.Count > 0)
		{
			rootTile = tilesQueue[tilesQueue.Count - 1];
		}
		else
		{
			rootTile = currentTile;
		}

		if (currentTile != null)
		{
			for (int i = 0; i < rootTile.neighbors.Length; ++i)
			{
				if (AllowedDirection((DIRECTIONS) i))
				{
					TileBase neighbor = rootTile.neighbors[i];
					clickableTiles.Add(neighbor);
				}
			}
		}
	}

	public bool IsTileCickable(TileBase tile)
	{
		return clickableTiles.Contains(tile);
	}

	private void OnTriggerEnter2D(Collider2D coll)
	{
		if (!coll.CompareTag("Wall"))
		{
			return;
		}
		endGame.SetActive(true);
		Destroy(gameObject);
	}

	private bool GoToTile()
	{
		if (TileReached())
		{
			return false;
		}
		Vector3 targetMove = tilesQueue[0].transform.position - transform.position;

		targetMove.z = 0;

		if (targetMove.magnitude < GetSpeed() * Time.deltaTime)
		{
			transform.position = new Vector3(tilesQueue[0].transform.position.x, tilesQueue[0].transform.position.y, transform.position.z);
			return true;
		}

		transform.position += targetMove.normalized * GetSpeed() * Time.deltaTime;
		return false;
	}

	private void OnDrawGizmos()
	{
		if (tilesQueue.Count <= 0)
		{
			return;
		}
		Gizmos.DrawLine(transform.position, tilesQueue[0].transform.position);

		for (int i = 0; i < tilesQueue.Count - 1; ++i)
		{
			Gizmos.DrawLine(tilesQueue[i].transform.position, tilesQueue[i + 1].transform.position);
		}
	}
}
