using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class RelicPlayer : MonoBehaviour
    {
		public int PlayerNumber = 0;

        public HoldingRelic HoldingRelicPrefab;

        [Range(0, 5)]
        public float CrushingDistance;

        public HoldingRelic HoldingRelic { get; set; }
        public PlayerInfo PlayerInfo { get; set; }

        private PlayerController playerController;

        private Transform leftHoldPosition;
        private Transform rightHoldPosition;

        private PlayerController.Direction? lastDirection;

        public void Awake()
        {
            playerController = GetComponent<PlayerController>();

            leftHoldPosition = transform.FindChild("RelicHoldPositionLeft");
            rightHoldPosition = transform.Find("RelicHoldPositionRight");
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
        }

        public void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("CrushingTrap"))
                CollideWithCrushingTrap(collision);
        }

        private void CollideWithCrushingTrap(Collision collision)
        {
            var normal = collision.contacts.First().normal;

            var allInRaycast = Physics.RaycastAll(new Ray(transform.position, normal), CrushingDistance);

            var hit = allInRaycast.Any(x => x.collider.gameObject.CompareTag("Player") == false);

            if (!hit)
                return;

            BeSquashed(collision.gameObject);
        }

        private void CollideWithDropPoint(Collision collision)
        {
            if (HoldingRelic == null || HoldingRelic.gameObject == null)
                return;

            Destroy(HoldingRelic.gameObject);
            HoldingRelic = null;
            lastDirection = null;

            collision.gameObject.GetComponent<DropPoint>().AcceptRelic(this);
        }

        private void CollideWithRelic(Collision collision)
        {
            var relic = collision.gameObject.GetComponent<Relic>();

            if (relic.CanPickUp == false)
                return;

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

            HoldingRelic.transform.localPosition = 
                playerController.LastRequestedDirection == PlayerController.Direction.Left ? 
                leftHoldPosition.localPosition : rightHoldPosition.localPosition;

            lastDirection = playerController.LastRequestedDirection;
        }

        public void SquashOtherPlayer(GameObject otherPlayer, Collision collision)
        {
            otherPlayer.GetComponent<RelicPlayer>().BeSquashed(gameObject);

            GetComponent<PlayerController>().DoBounceOnOtherPlayer(collision);
        }

        public void BeSquashed(GameObject squasher)
        {
            PlayerInfo.Spawner.Despawn(PlayerNumber);
            PlayerInfo.Spawner.SpawnAfterDelay(PlayerNumber);

            if (HoldingRelic != null)
            {
                HoldingRelic.DropAtPlayer();
                HoldingRelic = null;
            }
        }
    }
}