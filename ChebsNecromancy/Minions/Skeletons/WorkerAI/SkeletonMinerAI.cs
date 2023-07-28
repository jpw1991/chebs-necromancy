using System;
using ChebsValheimLibrary.Minions;
using ChebsValheimLibrary.Minions.AI;
using UnityEngine;

namespace ChebsNecromancy.Minions.Skeletons.WorkerAI
{
    public class SkeletonMinerAI : MinerAI
    {
        public override float UpdateDelay => SkeletonMinerMinion.UpdateDelay.Value;
        public override float LookRadius => SkeletonMinerMinion.LookRadius.Value;
        public override float RoamRange => UndeadMinion.RoamRange.Value;
        public override string RockInternalIDsList => SkeletonMinerMinion.RockInternalIDsList.Value;
        public override float ToolDamage => SkeletonMinerMinion.ToolDamage.Value;
        public override short ToolTier => SkeletonMinerMinion.ToolTier.Value;
        public override float ChatInterval => SkeletonMinerMinion.ChatInterval.Value;
        public override float ChatDistance => SkeletonMinerMinion.ChatDistance.Value;

        // private GameObject _minionTextObject;
        //
        // private Camera _camera;
        //
        // protected override void Awake()
        // {
        //     base.Awake();
        //
        //     _minionTextObject = new GameObject();
        //     _minionTextObject.AddComponent<TextMesh>();
        //     _minionTextObject.AddComponent<MeshRenderer>();
        //     _minionTextObject.transform.localScale = Vector3.one * 0.1f;
        //
        //     _camera = Camera.main;
        // }
        //
        // protected override void FixedUpdate()
        // {
        //     base.FixedUpdate();
        //
        //     var player = Player.m_localPlayer;
        //     if (player != null)
        //     {
        //         var position = transform.position;
        //         var newTextPosition = new Vector3(position.x - .25f, position.y + 2.5f, position.z);
        //         _minionTextObject.transform.position = newTextPosition;
        //         _minionText.Text = Status;
        //
        //         var playerPos = player.transform.position;
        //         var minionTextObjectPos = _minionTextObject.transform.position;
        //         if (_camera != null)
        //         {
        //             var cameraTransformPosition = _camera.transform.position;
        //             _minionTextObject.transform.LookAt(
        //                 new Vector3(minionTextObjectPos.x, cameraTransformPosition.y, minionTextObjectPos.z));
        //         }
        //         _minionTextObject.SetActive(
        //             Vector3.Distance(minionTextObjectPos, playerPos) < MinionText.RenderDistance);   
        //     }
        // }
        //
        // private void OnDestroy()
        // {
        //     Destroy(_minionTextObject);
        // }
    }
}