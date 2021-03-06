using System;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class RelicPlayer : MonoBehaviour
    {
        public enum DeathType
        {
            None,
            Player,
            Squash
        }

        public int PlayerNumber = 0;

        public HoldingRelic HoldingRelicPrefab;

        [Range(0, 5)]
        public float CrushingDistance;

        public HoldingRelic HoldingRelic { get; set; }
        public PlayerInfo PlayerInfo { get; set; }

        private PlayerController playerController;

        private Transform leftHoldPosition;
        private Transform rightHoldPosition;

        public GameObject SquishEffects;
        public GameObject DeathByPlayerEffects;

        public float CharacterHeight;

        private PlayerController.Direction? lastDirection;

        private CameraShaker cameraShaker;

        private Vector3 deathPosition;

        public void Awake()
        {
            playerController = GetComponent<PlayerController>();

            leftHoldPosition = transform.FindChild("RelicHoldPositionLeft");
            rightHoldPosition = transform.Find("RelicHoldPositionRight");

            cameraShaker = CameraShaker.Get();
        }

        public void OnCollisionEnter(Collision collision)
        {			
            if (collision.gameObject.CompareTag("Relic"))
            {
                CollideWithRelic(collision);
                return;
            }

            if (collision.gameObject.CompareTag("DropPoint"))
            {
                CollideWithDropPoint(collision);
            }

            if (collision.gameObject.CompareTag("CrushingTrap"))
                CollideWithCrushingTrap(collision);

			if (collision.gameObject.CompareTag("SpearTrap"))
				CollideWithSpearTrap();
        }

        public void OnCollisionStay(Collision collision)
        {

            if (collision.gameObject.CompareTag("CrushingTrap"))
                CollideWithCrushingTrap(collision);

			if (collision.gameObject.CompareTag("SpearTrap"))
				CollideWithSpearTrap();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("KillZone"))
                CollideWithKillZone();

			if (other.gameObject.CompareTag("SpearTrap"))
				CollideWithSpearTrap();

            if (other.gameObject.CompareTag ("WarpZone"))
                CollideWithTeleportZone (other);
        }

		public void OnTriggerStay(Collider other)
		{
			if (other.gameObject.CompareTag("LeverZone"))
				CollideWithLeverObject(other);

			if (other.gameObject.CompareTag("SpearTrap"))
				CollideWithSpearTrap();
		}

        private void CollideWithKillZone()
        {
            if (HoldingRelic)
            {
                HoldingRelic.RemoveAndRespawn();
                HoldingRelic = null;
            }

            Die(DeathType.None);
        }

		private void CollideWithLeverObject(Collider collider)
		{
			var leverController = collider.gameObject.GetComponent<leverScript>();
			if(leverController == null)
				throw new InvalidOperationException("LeverZone tagged object does not contain a LeverScript");

			if(Input.GetButtonDown("buttonX" + PlayerNumber))
			{
				leverController.Activate();
			}
		}

        private void CollideWithCrushingTrap(Collision collision)
        {
            var crushingTrap = collision.gameObject.GetComponent<trapScript>();
            if (crushingTrap.CurrentState == trapScript.STATE.WAITING)
                return;

            var normal = crushingTrap.LastMovementNormal;

            var center = new Vector3(transform.position.x, transform.position.y + CharacterHeight / 2f, transform.position.z);
            var allInRaycast = Physics.RaycastAll(new Ray(center, normal), CrushingDistance);

			var hit = allInRaycast.Any(x => x.collider.gameObject.CompareTag("Player") == false && x.collider.gameObject.CompareTag("Non-Interact") == false);
            
            if (!hit)
                return;

            BeSquashed(collision.gameObject, DeathType.Squash);

            var trapController = collision.gameObject.GetComponentInParent<trapScript>();
            if (trapController == null)
                throw new InvalidOperationException("huh?");

            trapController.StopCrushingAndReturn();
        }

        private void CollideWithTeleportZone(Collider col)
        {
            Transform offset = col.gameObject.GetComponent<teleportZoneScript> ().offset;
            Rigidbody rigid = GetComponent<Rigidbody> ();

            Debug.Log (offset.position);

            Vector3 tempRigidPos = rigid.position;
			tempRigidPos.y += offset.localPosition.y;
			tempRigidPos.x += offset.localPosition.x;
            rigid.position = tempRigidPos;
        }

		private void CollideWithSpearTrap()
		{
			BeSquashed(gameObject, DeathType.Squash);
		}

        private void CollideWithDropPoint(Collision collision)
        {
            if (HoldingRelic == null || HoldingRelic.gameObject == null)
                return;

            Destroy(HoldingRelic.gameObject);
            HoldingRelic = null;
            lastDirection = null;

			var dropPoint = collision.gameObject.GetComponent<DropPoint>();

			if(dropPoint == null)
				dropPoint = collision.gameObject.GetComponentInParent<DropPoint>();

            dropPoint.AcceptRelic(this);
        }

        private void CollideWithRelic(Collision collision)
        {
            var relic = collision.gameObject.GetComponent<Relic>();

            if (relic.CanPickUp == false)
                return;

            GeneralAudioController.PlaySound("RushPickup");

            var holdingRelic = relic.BeHeldBy(this);
            
            HoldingRelic = holdingRelic;
            HoldingRelic.RelicPlayer = this;
        }

        public void Update()
        {
            if (HoldingRelic == null)
                return;

            if (playerController.LastRequestedDirection == lastDirection)
                return;

            //HoldingRelic.transform.localPosition = 
            //    playerController.LastRequestedDirection == PlayerController.Direction.Left ? 
            //    leftHoldPosition.localPosition : rightHoldPosition.localPosition;

            lastDirection = playerController.LastRequestedDirection;
        }

        public void SquashOtherPlayer(GameObject otherPlayer, Collision collision)
        {
            otherPlayer.GetComponent<RelicPlayer>().BeSquashed(gameObject, DeathType.Player);

            GetComponent<PlayerController>().DoBounceOnOtherPlayer(collision);
        }

        public void Die(DeathType deathType)
        {
            deathPosition = transform.position;

            PlayerInfo.Spawner.Despawn(PlayerNumber);
            PlayerInfo.Spawner.SpawnAfterDelay(PlayerNumber);

            if (deathType == DeathType.Player)
                PlayerDeathEffects();
            else if (deathType == DeathType.Squash)
                SquashDeathEffects();
        }

        private void SquashDeathEffects()
        {
            cameraShaker.ShakeScreen(1, TimeSpan.FromSeconds(0.5f));

            if(SquishEffects != null)
                Instantiate(SquishEffects, deathPosition, Quaternion.identity);
        }

        private void PlayerDeathEffects()
        {
            cameraShaker.ShakeScreen(0.5f, TimeSpan.FromSeconds(0.5f));

            if(DeathByPlayerEffects != null)
                Instantiate(DeathByPlayerEffects, deathPosition, Quaternion.identity);
        }

        public void BeSquashed(GameObject squasher, DeathType deathType)
        {
            Die(deathType);

            if (HoldingRelic != null)
            {
                HoldingRelic.DropAtPlayer();
                HoldingRelic = null;
            }
        }

        public void StopInput()
        {
            GetComponent<PlayerController>().AllowInput = false;
        }
    }
}