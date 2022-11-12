using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using UnityEngine;
using DoNotModify;

namespace Hyperdrive {

	public class HyperdriveController : BaseSpaceShipController
	{
		private BehaviorTree currentBehaviorTree;
		
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
				MineInLigne(spaceship, data.Mines);
				ShootEnemyPerfectTime(spaceship, otherSpaceship);
			}

			// Get closest waypoint
			WayPointView closestWaypoint = GetClosestWaypoint(spaceship, data.WayPoints);

			float thrust = 1.0f;
			// float targetOrient = spaceship.Orientation;//spaceship.Orientation + 90.0f;
			
			
			
			bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);
			float targetOrient = AimingHelpers.ComputeSteeringOrient(spaceship, otherSpaceship.Position);

			bool shockwave = false;
			bool shoot = false;
			bool mine = false;

			SharedBool huntShoot = (SharedBool) currentBehaviorTree.GetVariable("Fire");

			if (huntShoot != null)
			{
				if (huntShoot.Value)
				{
					huntShoot.Value = false;
					shoot = true;
				}
			}

			SharedBool seekShock = (SharedBool) currentBehaviorTree.GetVariable("UseShockwave");

			if (seekShock != null)
			{
				if (seekShock.Value)
				{
					shockwave = true;
					seekShock.Value = false;
				}
			}
			
			return new InputData(thrust, targetOrient, needShoot, false, false);
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

		public void SetCurrentBehaviorTree(BehaviorTree behavior)
		{
			currentBehaviorTree = behavior;
		}

		private void MineInLigne (SpaceShipView us, List<MineView> allMines)
        {
			RaycastHit2D[] hit2D = Physics2D.RaycastAll(us.Position, us.LookAt);
			RaycastHit2D RaycastMine = hit2D.FirstOrDefault(raycastHit2D =>
			raycastHit2D.collider.transform.parent.gameObject.TryGetComponent(out Mine mine));

			if (RaycastMine.distance != 0 && AimingHelpers.CanHit(us, RaycastMine.transform.position, 0.15f))
				currentBehaviorTree.GetVariable("Mine").SetValue(true);
			else
				currentBehaviorTree.GetVariable("Mine").SetValue(false);


		}

		private void ShootEnemyPerfectTime(SpaceShipView us, SpaceShipView enemy)
		{
			//GameObject.Find("SpaceShip" + (enemy.Owner + 1)).GetComponent<SpaceShip>().IsStun();
			float currentDistance = (us.Position - enemy.Position).sqrMagnitude;
			float time = currentDistance / Bullet.Speed;

			if (time < enemy.HitCountdown && AimingHelpers.CanHit(us, enemy.Position, 0.15f) && us.Energy > 0.40f)
				currentBehaviorTree.GetVariable("EnemyInvulnerability").SetValue(true);
			else
				currentBehaviorTree.GetVariable("EnemyInvulnerability").SetValue(false);

		}

	}
}

}
