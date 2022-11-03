using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using UnityEngine;
using DoNotModify;

namespace Hyperdrive {

	public class HyperdriveController : BaseSpaceShipController
	{
		private ExternalBehavior currentBehaviorTree;
		
		public override void Initialize(SpaceShipView spaceship, GameData data)
		{
		}

		public override InputData UpdateInput(SpaceShipView spaceship, GameData data)
		{
			SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);

			if (currentBehaviorTree != null)
			{
				UpdateEnemyDistanceVariables(spaceship, otherSpaceship);
				UpdateOwnEnergyVariables(spaceship);
				UpdateClosestMineVariables(spaceship, data.Mines);
			}

			// Get closest waypoint
			WayPointView closestWaypoint = GetClosestWaypoint(spaceship, data.WayPoints);
			
			// Get first asteroid in front of us
			RaycastHit2D[] hit2D = Physics2D.RaycastAll(spaceship.Position, spaceship.LookAt);
			Debug.DrawRay(spaceship.Position, spaceship.LookAt, Color.red);
			RaycastHit2D asteroid = hit2D.FirstOrDefault(raycastHit2D =>
				raycastHit2D.collider.transform.parent.gameObject.TryGetComponent(out Asteroid asteroid));
			if (asteroid.distance != 0.0f)
			{
				Debug.Log($"Raycast hit an asteroid {asteroid.distance}");
			}

			Debug.Log($"Closest waypoint is {(closestWaypoint.Position - spaceship.Position).sqrMagnitude} ");
			
			if (asteroid.distance <= (closestWaypoint.Position - spaceship.Position).sqrMagnitude)
			{
				Debug.Log($"target is blocked by asteroids");
			}
			
			float thrust = 0.0f;
			float targetOrient = spaceship.Orientation;//spaceship.Orientation + 90.0f;
			// bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
			// float targetOrient = AimingHelpers.ComputeSteeringOrient(spaceship, otherSpaceship.Position);

			if (Input.GetKey(KeyCode.O))
			{
				thrust = 1.0f;
			}

			if (Input.GetKey(KeyCode.M))
			{
				targetOrient += 20.0f;
			} else if (Input.GetKey(KeyCode.K))
			{
				targetOrient -= 20.0f;
			}
			
			return new InputData(thrust, targetOrient, false, false, false);
		}

		private WayPointView GetClosestWaypoint(SpaceShipView us, List<WayPointView> wayPointViews)
		{
			WayPointView closestWaypoint = null;
			float minWaypointDistance = float.MaxValue;
			foreach (WayPointView wayPV in wayPointViews)
			{
				float currentDistance = (us.Position - wayPV.Position).sqrMagnitude;
				if (currentDistance < minWaypointDistance)
				{
					minWaypointDistance = currentDistance;
					closestWaypoint = wayPV;
				}
			}

			return closestWaypoint;
		}

		private void UpdateEnemyDistanceVariables(SpaceShipView us, SpaceShipView enemy)
		{
			float distanceBetweenUserAndEnemy = (us.Position - enemy.Position).sqrMagnitude;
			
			SharedFloat seekEnemyRange = (SharedFloat)currentBehaviorTree.GetVariable("Enemy Range");
			
			// Check if variable exist
			if (seekEnemyRange != null)
				seekEnemyRange.Value = distanceBetweenUserAndEnemy;
		}

		private void UpdateOwnEnergyVariables(SpaceShipView us)
		{
			SharedFloat seekPlayerEnergy = (SharedFloat)currentBehaviorTree.GetVariable("Player Energy");
			
			// Check if variable exist
			if (seekPlayerEnergy != null)
				seekPlayerEnergy.Value = us.Energy;
		}
		
		private void UpdateClosestMineVariables(SpaceShipView us, List<MineView> allMines)
		{
			float minDistance = float.MaxValue;
			foreach (var mine in allMines)
			{
				float currentDistance = (us.Position - mine.Position).sqrMagnitude;
				if (currentDistance < minDistance)
					minDistance = currentDistance;
			}
			
			SharedFloat seekMineRange = (SharedFloat)currentBehaviorTree.GetVariable("Mine Range");
			
			// Check if variable exist
			if (seekMineRange != null)
				seekMineRange.Value = minDistance;
		}

		public void SetCurrentBehaviorTree(ExternalBehavior behavior)
		{
			currentBehaviorTree = behavior;
		}
	}

}
