using System;
using Eleon.Modding;
using EmpyrionAPITools;
using System.Collections.Generic;
using EmpyrionAPIDefinitions;
using System.Linq;
using System.Numerics;

namespace EmpyrionBaseAlign
{
    public partial class EmpyrionBaseAlign : SimpleMod
    {
        public ModGameAPI GameAPI { get; set; }
        public int BaseToAlignId { get; private set; }
        public int MainBaseId { get; private set; }
        public IdPositionRotation BaseToAlign { get; private set; }
        public IdPositionRotation MainBase { get; private set; }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            this.verbose = true;

            this.log($"**HandleEmpyrionBaseAlign loaded");

            Event_Entity_PosAndRot += EmpyrionBaseAlign_Event_Entity_PosAndRot;

            this.ChatCommands.Add(new ChatCommand(@"/al", ExecAlignCommand, "Hilfe"));
            this.ChatCommands.Add(new ChatCommand(@"/al (?<BaseToAlignId>.+) (?<MainBaseId>.+)", ExecAlignCommand, "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten"));
        }

        private void EmpyrionBaseAlign_Event_Entity_PosAndRot(IdPositionRotation aData)
        {
            if (aData.id == MainBaseId   ) MainBase    = aData;
            if (aData.id == BaseToAlignId) BaseToAlign = aData;

            if (MainBase == null || BaseToAlign == null) return;

            ExecAlign(MainBase, BaseToAlign);

            this.log($"**HandleEmpyrionBaseAlign:Align {BaseToAlign.id} pos= {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} rot= {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z}");
            GameAPI.Game_Request(CmdId.Request_Entity_Teleport, 1, BaseToAlign);
        }

        enum ChatType
        {
            Faction = 3,
            Global = 5,
        }

        private void ExecAlignCommand(ChatInfo info, Dictionary<string, string> args)
        {
            this.log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}");

            if (info.type == (byte)ChatType.Faction) return;
            if(args.Count < 2) { DisplayHelp(info); return; }

            BaseToAlignId = getIntParam(args, "BaseToAlignId");
            MainBaseId    = getIntParam(args, "MainBaseId");

            MainBase = BaseToAlign = null;

            GetEntity_PosAndRot(BaseToAlignId);
            GetEntity_PosAndRot(MainBaseId);
        }

        private int getIntParam(Dictionary<string, string> aArgs, string aParameterName)
        {
            string valueStr;
            if (!aArgs.TryGetValue(aParameterName, out valueStr)) return 0;

            int value;
            if (!int.TryParse(valueStr, out value)) return 0;

            return value;
        }

        private void DisplayHelp(ChatInfo info)
        {
            this.Request_Player_Info(info.playerId.ToId(), (I) =>
            {
                var playerPermissionLevel = (PermissionType)I.permission;
                var header = $"Befehle für {I.playerName} mit den Rechten {playerPermissionLevel}\n";

                var lines = this.GetChatCommandsForPermissionLevel(playerPermissionLevel)
          .Select(x => x.ToString())
          .OrderBy(x => x.Length).ToList();

                lines.Insert(0, header);
                lines.Add("/al => Hilfe anzeigen");

                var msg = new DialogBoxData()
                {
                    Id = info.playerId,
                    MsgText = String.Join("\n", lines.ToArray())
                };
                Request_ShowDialog_SinglePlayer(msg);
            });
        }

        private void GetEntity_PosAndRot(int aId)
        {
            GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, 1, new Eleon.Modding.Id(aId));
        }

        private Vector3 GetVector3(PVector3 aVector)
        {
            return new Vector3(aVector.x, aVector.y, aVector.z);
        }

        private Matrix4x4 GetMatrix4x4(PVector3 aVector)
        {
            return Matrix4x4.CreateFromYawPitchRoll(aVector.x, aVector.y, aVector.z);
        }

        private Matrix4x4 GetMatrix4x4Neg(PVector3 aVector)
        {
            return Matrix4x4.CreateFromYawPitchRoll(-aVector.x, -aVector.y, -aVector.z);
        }

        private void ExecAlign(IdPositionRotation aMainBase, IdPositionRotation aBaseToAlign)
        {
            this.log($"**HandleEmpyrionBaseAlign:ExecAlign {aMainBase.id} pos= {aMainBase.pos.x},{aMainBase.pos.y},{aMainBase.pos.z} rot= {aMainBase.rot.x},{aMainBase.rot.y},{aMainBase.rot.z} Align: {aBaseToAlign.id} pos= {aBaseToAlign.pos.x},{aBaseToAlign.pos.y},{aBaseToAlign.pos.z} rot= {aBaseToAlign.rot.x},{aBaseToAlign.rot.y},{aBaseToAlign.rot.z}");

            var posHomeBase  = GetVector3(aMainBase.pos);
            var posAlignBase = GetVector3(aBaseToAlign.pos);

            var posHomeBaseRot     = GetMatrix4x4Neg(aMainBase.rot);
            var posHomeBaseRotBack = GetMatrix4x4   (aMainBase.rot);

            var posNormAlignBaseTrans = posAlignBase - posHomeBase;
            var posNormAlignBaseRot = Vector3.Transform(posNormAlignBaseTrans, posHomeBaseRot);
            posNormAlignBaseRot = new Vector3((int)(posNormAlignBaseRot.X + 1) / 2 * 2, 
                                              (int)(posNormAlignBaseRot.Y + 1) / 2 * 2, 
                                              (int)(posNormAlignBaseRot.Z + 1) / 2 * 2);
            var posNormAlignBaseRotBack = Vector3.Transform(posNormAlignBaseRot, posHomeBaseRotBack);
            var posNormAlignBaseRotBackTans = posNormAlignBaseRotBack + posHomeBase;

            aBaseToAlign.pos.x = posNormAlignBaseRotBackTans.X;
            aBaseToAlign.pos.y = posNormAlignBaseRotBackTans.Y;
            aBaseToAlign.pos.z = posNormAlignBaseRotBackTans.Z;

            aBaseToAlign.rot.x = aMainBase.rot.x;
            aBaseToAlign.rot.y = aMainBase.rot.y;
            aBaseToAlign.rot.z = aMainBase.rot.z;
        }


    }
}
