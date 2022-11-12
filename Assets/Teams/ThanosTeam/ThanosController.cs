using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoNotModify;
using UnityEditor;

namespace ThanosTeam {

	public class ThanosController : BaseSpaceShipController
	{
		private int currentWaypoint;

		[SerializeField] private GameObject thanosSpawn;
		[SerializeField] private GameObject ExempleShip;
		
		public override void Initialize(SpaceShipView spaceship, GameData data)
		{
		}

		public override InputData UpdateInput(SpaceShipView spaceship, GameData data)
		{
			SpaceShipView otherSpaceship = data.GetSpaceShipForOwner(1 - spaceship.Owner);
			float thrust = 1.0f;
			float targetOrient = spaceship.Orientation + 90.0f;
			bool needShoot = AimingHelpers.CanHit(spaceship, otherSpaceship.Position, otherSpaceship.Velocity, 0.15f);

			if (data.timeLeft <= 40.0f)
			{
				GameObject thanos = GameObject.Find("SpaceShip" + (spaceship.Owner + 1));
				GameObject victime = GameObject.Find("SpaceShip" + (otherSpaceship.Owner + 1));

				if (data.timeLeft < 10f && Random.Range(0.0f, 1.0f) >= .99f)
				{
					for (int i = 0; i < 100; i++)
					{
						Instantiate(thanosSpawn, new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), 1), Quaternion.identity);
						// Instantiate(ExempleShip, new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), 1), Quaternion.identity);
					}
				}

				thanos.transform.position = data.WayPoints[currentWaypoint].Position;

				currentWaypoint++;

				if (currentWaypoint >= data.WayPoints.Count)
					currentWaypoint = 0;
			}
			
			return new InputData(thrust, targetOrient, needShoot, false, false);
		}
	}

}
