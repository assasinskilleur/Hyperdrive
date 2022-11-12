using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using UnityEngine;
using DoNotModify;

namespace Hyperdrive
{
    public class HyperdriveController : BaseSpaceShipController
    {
        private BehaviorTree currentBehaviorTree;
        private Animator _animator;

        public override void Initialize(SpaceShipView spaceship, GameData data)
        {
            _animator = GetComponent<Animator>();
        }

        public override InputData UpdateInput(SpaceShipView spaceship, GameData data)
        {
            SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);

            if (currentBehaviorTree != null)
            {
                UpdateAnimatorBlackboard(data);
                UpdateEnemyDistanceVariables(spaceship, otherSpaceship);
                UpdateOwnEnergyVariables(spaceship);
                UpdateClosestMineVariables(spaceship, data.Mines);
                MineInLigne(spaceship, data.Mines);
                ShootEnemyPerfectTime(spaceship, otherSpaceship);

                // Get closest waypoint
                WayPointView closestWaypoint = GetClosestWaypoint(spaceship, data.WayPoints);

                if (data.timeLeft >= 10.0f)
                {
                    SharedBool huntAsteroid = (SharedBool)currentBehaviorTree.GetVariable("Asteroid");
                    if (huntAsteroid != null)
                    {
                        huntAsteroid.SetValue(HasAsteroidInWay(spaceship, otherSpaceship.Position));
                    }
                }


                SharedBool seekAsteroid = (SharedBool)currentBehaviorTree.GetVariable("Asteroid in the way");
                if (seekAsteroid != null)
                {
                    seekAsteroid.SetValue(HasAsteroidInWay(spaceship, closestWaypoint.Position));
                }


                bool shockwave = false;
                bool shoot = false;
                bool mine = false;
                
                
                float thrust = 1.0f;
                // float targetOrient = spaceship.Orientation;//spaceship.Orientation + 90.0f;

                bool needShoot =
                    AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 5f);
                float targetOrient = AimingHelpers.ComputeSteeringOrient(spaceship, otherSpaceship.Position);

                SharedBool huntShoot = (SharedBool)currentBehaviorTree.GetVariable("Fire");

                if (huntShoot != null)
                {
                    if (huntShoot.Value)
                    {
                        huntShoot.Value = false;
                        shoot = true;
                    }
                }

                SharedBool seekShock = (SharedBool)currentBehaviorTree.GetVariable("UseShockwave");

                if (seekShock != null)
                {
                    if (seekShock.Value)
                    {
                        shockwave = true;
                        seekShock.Value = false;
                    }
                }
                
                SharedBool goToWayPoint = (SharedBool)currentBehaviorTree.GetVariable("GoToWaypoint");

                if (goToWayPoint != null)
                {
                    if (goToWayPoint.Value)
                    {
                        targetOrient = AimingHelpers.ComputeSteeringOrient(spaceship, closestWaypoint.Position);
                    }
                }

                return new InputData(thrust, targetOrient, shoot && needShoot, false, shockwave);
            }


            return new InputData(0.0f, spaceship.Orientation, false, false, false);
        }

        private void UpdateAnimatorBlackboard(GameData data)
        {
            _animator.SetFloat("CurrentTimer", data.timeLeft);
        }

        private WayPointView GetClosestWaypoint(SpaceShipView us, List<WayPointView> wayPointViews)
        {
            WayPointView closestWaypoint = null;
            float minWaypointDistance = float.MaxValue;
            foreach (WayPointView wayPV in wayPointViews)
            {
                float currentDistance = Vector2.Distance(us.Position, wayPV.Position);
                if (currentDistance < minWaypointDistance)
                {
                    if (wayPV.Owner != us.Owner)
                    {
                        minWaypointDistance = currentDistance;
                        closestWaypoint = wayPV;
                    }
                }
            }

            return closestWaypoint;
        }

        private bool HasAsteroidInWay(SpaceShipView us, Vector2 target)
        {
            bool hasAsteroidInWay = false;

            RaycastHit2D[] hit2D = Physics2D.RaycastAll(us.Position, (target - us.Position));
            RaycastHit2D RaycastAsteroid = hit2D.FirstOrDefault(raycastHit2D =>
                raycastHit2D.collider.transform.parent.gameObject.TryGetComponent(out Asteroid asteroid));

            if (RaycastAsteroid.distance > 0)
                hasAsteroidInWay = true;

            return hasAsteroidInWay;
        }

        private void UpdateEnemyDistanceVariables(SpaceShipView us, SpaceShipView enemy)
        {
            float distanceBetweenUserAndEnemy = Vector2.Distance(us.Position, enemy.Position);

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
                float currentDistance = Vector2.Distance(us.Position, mine.Position);
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

        private void MineInLigne(SpaceShipView us, List<MineView> allMines)
        {
            RaycastHit2D[] hit2D = Physics2D.RaycastAll(us.Position, us.LookAt);
            RaycastHit2D RaycastMine = hit2D.FirstOrDefault(raycastHit2D =>
                raycastHit2D.collider.transform.parent.gameObject.TryGetComponent(out Mine mine));

            if (RaycastMine.distance != 0 && AimingHelpers.CanHit(us, RaycastMine.transform.position, 0.15f))
                currentBehaviorTree.GetVariable("Mine")?.SetValue(true);
            else
                currentBehaviorTree.GetVariable("Mine")?.SetValue(false);
        }

        private void ShootEnemyPerfectTime(SpaceShipView us, SpaceShipView enemy)
        {
            //GameObject.Find("SpaceShip" + (enemy.Owner + 1)).GetComponent<SpaceShip>().IsStun();
            float currentDistance = Vector2.Distance(us.Position, enemy.Position);
            float time = currentDistance / Bullet.Speed;
      
            if (enemy.HitCountdown <= time)
                currentBehaviorTree.GetVariable("EnemyInvulnerability")?.SetValue(false);
            else
                currentBehaviorTree.GetVariable("EnemyInvulnerability")?.SetValue(true);
        }
    }
}