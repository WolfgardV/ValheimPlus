using System.Collections.Generic;
using UnityEngine;

namespace ValheimPlus.Lift
{
    public class Lift : MonoBehaviour
    {
        private static List<Lift> m_currentLifts = new List<Lift>();
        
        private List<Player> m_players = new List<Player>();
        private bool m_forwardPressed;
        private bool m_backwardPressed;
        public LiftControlls m_liftControlls;
        private Lift.Speed m_speed;
        public float m_forwardForce = 50f;
        private Rigidbody m_body;
        private ZNetView m_nview;
        
        private void Awake()
        {
            this.m_nview = this.GetComponent<ZNetView>();
            this.m_body = this.GetComponent<Rigidbody>();

            if (this.m_nview.GetZDO() == null)
                this.enabled = false;
            this.m_body.maxDepenetrationVelocity = 2f;
            Heightmap.ForceGenerateAll();
        }

        public bool CanBeRemoved()
        {
            return this.m_players.Count == 0;
        }

        private void Start()
        {
            this.m_nview.Register("Stop", RPC_Stop);
            this.m_nview.Register("Forward", RPC_Forward);
            this.m_nview.Register("Backward", RPC_Backward);
            this.InvokeRepeating("UpdateOwner", 2f, 2f);
        }

        private void PrintStats()
        {
            if (this.m_players.Count == 0)
                return;
            ZLog.Log((object) ("Vel:" + this.m_body.velocity.magnitude.ToString("0.0")));
        }

        public void ApplyMovementControlls(Vector3 dir)
        {
            bool flag1 = (double) dir.z > 0.5;
            bool flag2 = (double) dir.z < -0.5;
            if (flag1 && !this.m_forwardPressed)
                this.Forward();
            if (flag2 && !this.m_backwardPressed)
                this.Backward();
            
            this.m_forwardPressed = flag1;
            this.m_backwardPressed = flag2;
        }

        public void Forward()
        {
            this.m_nview.InvokeRPC(nameof(Forward), new object[0]);
        }

        public void Backward()
        {
            this.m_nview.InvokeRPC(nameof(Backward), new object[0]);
        }

        public void Stop()
        {
            this.m_nview.InvokeRPC(nameof(Stop), new object[0]);
        }

        private void RPC_Stop(long sender)
        {
            this.m_speed = Lift.Speed.Stop;
        }

        private void RPC_Forward(long sender)
        {
            switch (this.m_speed)
            {
                case Lift.Speed.Stop:
                    this.m_speed = Lift.Speed.Forward;
                    break;
                case Lift.Speed.Back:
                    this.m_speed = Lift.Speed.Stop;
                    break;
            }
        }

        private void RPC_Backward(long sender)
        {
            switch (this.m_speed)
            {
                case Lift.Speed.Stop:
                    this.m_speed = Lift.Speed.Back;
                    break;
                case Lift.Speed.Forward:
                    this.m_speed = Lift.Speed.Stop;
                    break;
            }
        }

        private void FixedUpdate()
        {
            bool haveControllingPlayer = this.HaveControllingPlayer();
            this.UpdateControlls(Time.fixedDeltaTime);
            if ((bool) (UnityEngine.Object) this.m_nview && !this.m_nview.IsOwner())
                return;
            
            if (this.m_players.Count == 0)
            {
                this.m_speed = Lift.Speed.Stop;
            }

            if (!haveControllingPlayer && (this.m_speed == Lift.Speed.Forward || this.m_speed == Lift.Speed.Back))
                this.m_speed = Lift.Speed.Stop;
            
            this.m_body.velocity = this.transform.forward * this.m_forwardForce;

            Vector3 zero = Vector3.zero;
            switch (this.m_speed)
            {
                case Lift.Speed.Back:
                    zero -= -this.transform.forward * this.m_forwardForce;
                    break;
                case Lift.Speed.Forward:
                    zero += this.transform.forward * this.m_forwardForce;
                    break;
            }
            
            this.m_body.AddForceAtPosition(zero * Time.fixedDeltaTime, this.transform.position, ForceMode.VelocityChange);
            this.ApplyEdgeForce(Time.fixedDeltaTime);
        }

        private void ApplyEdgeForce(float dt)
        {
            float magnitude = this.transform.position.magnitude;
            float l = 10420f;
            if ((double) magnitude <= (double) l)
                return;
            this.m_body.AddForce(
                Vector3.Normalize(this.transform.position) * (Utils.LerpStep(l, 10500f, magnitude) * 8f) * dt,
                ForceMode.VelocityChange);
        }

        private void UpdateControlls(float dt)
        {
            if (this.m_nview.IsOwner())
            {
                this.m_nview.GetZDO().Set("forward", (int) this.m_speed);
            }
            else
            {
                this.m_speed = (Lift.Speed) this.m_nview.GetZDO().GetInt("forward", 0);
            }
        }

        private void UpdateOwner()
        {
            if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() ||
                ((UnityEngine.Object) Player.m_localPlayer == (UnityEngine.Object) null || this.m_players.Count <= 0) ||
                this.IsPlayerInLift(Player.m_localPlayer))
                return;
            long owner = this.m_players[0].GetOwner();
            this.m_nview.GetZDO().SetOwner(owner);
            ZLog.Log((object) ("Changing lift owner to " + (object) owner));
        }

        private void OnTriggerEnter(Collider collider)
        {
            Player component = collider.GetComponent<Player>();
            if (!(bool) (UnityEngine.Object) component)
                return;
            this.m_players.Add(component);
            ZLog.Log((object) ("Player on lift, total on lift " + (object) this.m_players.Count));
            if (!((UnityEngine.Object) component == (UnityEngine.Object) Player.m_localPlayer))
                return;
            Lift.m_currentLifts.Add(this);
        }

        private void OnTriggerExit(Collider collider)
        {
            Player component = collider.GetComponent<Player>();
            if (!(bool) (UnityEngine.Object) component)
                return;
            this.m_players.Remove(component);
            ZLog.Log((object) ("Player over lift, players left " + (object) this.m_players.Count));
            if (!((UnityEngine.Object) component == (UnityEngine.Object) Player.m_localPlayer))
                return;
            Lift.m_currentLifts.Remove(this);
        }

        public bool IsPlayerInLift(ZDOID zdoid)
        {
            foreach (Character player in this.m_players)
            {
                if (player.GetZDOID() == zdoid)
                    return true;
            }

            return false;
        }

        public bool IsPlayerInLift(Player player)
        {
            return this.m_players.Contains(player);
        }

        public bool HasPlayerOnLift()
        {
            return this.m_players.Count > 0;
        }

        private void OnDestroy()
        {
            if (this.m_nview.IsValid() && this.m_nview.IsOwner())
                Gogan.LogEvent("Game", "LiftDestroyed", this.gameObject.name, 0L);
            Lift.m_currentLifts.Remove(this);
        }

        private void OnDestroyed()
        {
            
        }


        public bool HaveControllingPlayer()
        {
            return this.m_players.Count != 0 && this.m_liftControlls.HaveValidUser();
        }

        public bool IsOwner()
        {
            return this.m_nview.IsValid() && this.m_nview.IsOwner();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(this.transform.position + this.transform.forward * this.m_forwardForce, 0.25f);
        }

        public enum Speed
        {
            Stop,
            Back,
            Forward,
        }
    }
}