using System;
using UnityEngine;

namespace ValheimPlus.Lift
{
  public class LiftControlls : MonoBehaviour, Interactable, Hoverable
  {
    public string m_hoverText = "";
    public float m_maxUseRange = 10f;
    public Vector3 m_detachOffset = new Vector3(0.0f, 0.5f, 0.0f);
    public string m_attachAnimation = "attach_chair";
    public Lift m_lift;
    public Transform m_attachPoint;
    private ZNetView m_nview;

    private void Awake()
    {
      this.m_nview = this.m_lift.GetComponent<ZNetView>();
      this.m_nview.Register<ZDOID>("LiftRequestControl", new Action<long, ZDOID>(this.RPC_LiftRequestControl));
      this.m_nview.Register<ZDOID>("LiftReleaseControl", new Action<long, ZDOID>(this.RPC_LiftReleaseControl));
      this.m_nview.Register<bool>("LiftRequestRespons", new Action<long, bool>(this.RPC_LiftRequestRespons));
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
      return false;
    }

    public bool Interact(Humanoid character, bool repeat)
    {
      if (repeat || !this.m_nview.IsValid() || !this.InUseDistance(character))
        return false;
      Player player = character as Player;
      if ((UnityEngine.Object) player == (UnityEngine.Object) null || player.IsEncumbered() ||
          (UnityEngine.Object) GetStandingOnLift(player) != (UnityEngine.Object) this.m_lift)
        return false;
      this.m_nview.InvokeRPC("LiftRequestControl", (object) player.GetZDOID());
      return false;
    }
    
    public Lift GetStandingOnLift(Player player)
    {
      if (!player.IsOnGround())
        return (Lift) null;
      return (bool) (UnityEngine.Object) player.m_lastGroundBody ? player.m_lastGroundBody.GetComponent<Lift>() : (Lift) null;
    }

    public Lift GetLift()
    {
      return this.m_lift;
    }

    public string GetHoverText()
    {
      return !this.InUseDistance((Humanoid) Player.m_localPlayer)
        ? Localization.instance.Localize("<color=grey>$piece_toofar</color>")
        : Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
    }

    public string GetHoverName()
    {
      return Localization.instance.Localize(this.m_hoverText);
    }

    private void RPC_LiftRequestControl(long sender, ZDOID playerID)
    {
      if (!this.m_nview.IsOwner() || !this.m_lift.IsPlayerInLift(playerID))
        return;
      if (this.GetUser() == playerID || !this.HaveValidUser())
      {
        this.m_nview.GetZDO().Set("user", playerID);
        this.m_nview.InvokeRPC(sender, "LiftRequestRespons", true);
      }
      else
        this.m_nview.InvokeRPC(sender, "LiftRequestRespons", false);
    }

    private void RPC_LiftReleaseControl(long sender, ZDOID playerID)
    {
      if (!this.m_nview.IsOwner() || !(this.GetUser() == playerID))
        return;
      this.m_nview.GetZDO().Set("user", ZDOID.None);
    }

    private void RPC_LiftRequestRespons(long sender, bool granted)
    {
      if (!(bool) (UnityEngine.Object) Player.m_localPlayer)
        return;
      if (granted)
      {
        //Player.m_localPlayer.StartLiftControl(this);
        if (!((UnityEngine.Object) this.m_attachPoint != (UnityEngine.Object) null))
          return;
        Player.m_localPlayer.AttachStart(this.m_attachPoint, false, false, this.m_attachAnimation, this.m_detachOffset);
      }
      else
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, (Sprite) null);
    }

    public void OnUseStop(Player player)
    {
      if (!this.m_nview.IsValid())
        return;
      this.m_nview.InvokeRPC("LiftReleaseControl", (object) player.GetZDOID());
      if (!((UnityEngine.Object) this.m_attachPoint != (UnityEngine.Object) null))
        return;
      player.AttachStop();
    }

    public bool HaveValidUser()
    {
      ZDOID user = this.GetUser();
      return !user.IsNone() && this.m_lift.IsPlayerInLift(user);
    }

    public bool IsLocalUser()
    {
      if (!(bool) (UnityEngine.Object) Player.m_localPlayer)
        return false;
      ZDOID user = this.GetUser();
      return !user.IsNone() && user == Player.m_localPlayer.GetZDOID();
    }

    private ZDOID GetUser()
    {
      return !this.m_nview.IsValid() ? ZDOID.None : this.m_nview.GetZDO().GetZDOID("user");
    }

    private bool InUseDistance(Humanoid human)
    {
      return (double) Vector3.Distance(human.transform.position, this.m_attachPoint.position) <
             (double) this.m_maxUseRange;
    }
  }
}