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
        public Vector3 ShiftVector { get; private set; }
        public IdPositionRotation BaseToAlign { get; private set; }
        public IdPositionRotation MainBase { get; private set; }
        public bool WithinAlign { get; private set; }

        public Dictionary<int, IdPositionRotation> OriginalPosRot { get; set; } = new Dictionary<int, IdPositionRotation>();

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            this.verbose = true;

            this.log($"**HandleEmpyrionBaseAlign loaded");

            Event_Entity_PosAndRot += EmpyrionBaseAlign_Event_Entity_PosAndRot;

            this.ChatCommands.Add(new ChatCommand(@"/al", ExecAlignCommand, "Hilfe"));
            this.ChatCommands.Add(new ChatCommand(@"/al:undo (?<BaseToAlignId>\d+)", ExecAlignCommand, "UNDO: Basis {BaseToAlignId} ausrichten"));
            this.ChatCommands.Add(new ChatCommand(@"/al (?<BaseToAlignId>\d+) (?<MainBaseId>\d+) (?<ShiftX>.+),(?<ShiftY>.+),(?<ShiftZ>.+)", ExecAlignCommand, "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten und um {ShiftX},{ShiftY},{ShiftZ} verschieben"));
            this.ChatCommands.Add(new ChatCommand(@"/al (?<BaseToAlignId>\d+) (?<MainBaseId>\d+)", ExecAlignCommand, "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten"));
        }

        private void EmpyrionBaseAlign_Event_Entity_PosAndRot(IdPositionRotation aData)
        {
            if (aData.id == MainBaseId   ) MainBase    = aData;
            if (aData.id == BaseToAlignId) BaseToAlign = aData;

            if ((MainBase == null && MainBaseId != 0) || BaseToAlign == null || WithinAlign) return;
            WithinAlign = true;

            if (!OriginalPosRot.ContainsKey(BaseToAlign.id)) OriginalPosRot.Add(BaseToAlign.id, BaseToAlign);

            var AlignResult = BaseToAlign = OriginalPosRot[BaseToAlign.id];
            
            if (MainBaseId != 0)
            {
                this.log($"**HandleEmpyrionBaseAlign:ExecAlign {MainBase.id} pos= {MainBase.pos.x},{MainBase.pos.y},{MainBase.pos.z} rot= {MainBase.rot.x},{MainBase.rot.y},{MainBase.rot.z} Align: {BaseToAlign.id} pos= {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} rot= {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} Shift={ShiftVector.X},{ShiftVector.Y},{ShiftVector.Z}");
                AlignResult = ExecAlign(MainBase, BaseToAlign, ShiftVector);
            }

            this.log($"**HandleEmpyrionBaseAlign:Align {(MainBaseId == 0 ? "UNDO" : "")} setposition {BaseToAlign.id} {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} setrotation {BaseToAlign.id} {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} -> \n" +
                     $"setposition {BaseToAlign.id} {AlignResult.pos.x},{AlignResult.pos.y},{AlignResult.pos.z} setrotation {BaseToAlign.id} {AlignResult.rot.x},{AlignResult.rot.y},{AlignResult.rot.z}");
            GameAPI.Game_Request(CmdId.Request_Entity_Teleport, 1, AlignResult);
            WithinAlign = false;
        }

        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private void ExecAlignCommand(ChatInfo info, Dictionary<string, string> args)
        {
            this.log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}");

            if (info.type != (byte)ChatType.Faction) return;
            if (args.Count < 2) { DisplayHelp(info); return; }

            BaseToAlignId = getIntParam(args, "BaseToAlignId");
            MainBaseId    = getIntParam(args, "MainBaseId");

            ShiftVector = new Vector3(getIntParam(args, "ShiftX"), getIntParam(args, "ShiftY"), getIntParam(args, "ShiftZ"));

            MainBase = BaseToAlign = null;
            WithinAlign = false;

            CheckPlayerPermissionThenExecAlign(info);
        }

        private void CheckPlayerPermissionThenExecAlign(ChatInfo info)
        {
            this.Request_Player_Info(info.playerId.ToId(), (I) =>
            {
                var playerPermissionLevel = (PermissionType)I.permission;

                if (playerPermissionLevel > PermissionType.Player) GetPosAndRotThenExecAlign();
                else
                    this.Request_Structure_BlockStatistics(new Id(BaseToAlignId), O =>
                    {
                        if (O.blockStatistics.Aggregate(0, (s, i) => s + i.Value) > 10) this.log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {O.blockStatistics.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")} -> zu viele Blöcke");
                        else                                                            GetPosAndRotThenExecAlign();
                    });
            });
        }

        private void GetPosAndRotThenExecAlign()
        {
            GetEntity_PosAndRot(BaseToAlignId);
            if(MainBaseId != 0) GetEntity_PosAndRot(MainBaseId);
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
            GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, 1, new Id(aId));
        }

        public static Vector3 GetVector3(PVector3 aVector)
        {
            return new Vector3(aVector.x, aVector.y, aVector.z);
        }

        public static PVector3 GetVector3(Vector3 aVector)
        {
            return new PVector3(aVector.X, aVector.Y, aVector.Z);
        }

        public static Matrix4x4 GetMatrix4x4(PVector3 aVector)
        {
            return Matrix4x4.CreateFromYawPitchRoll(aVector.y * (float)(Math.PI / 180), aVector.z * (float)(Math.PI / 180), aVector.x * (float)(Math.PI / 180));
        }

        public static IdPositionRotation ExecAlign(IdPositionRotation aMainBase, IdPositionRotation aBaseToAlign, Vector3 aShiftVector)
        {
            var posHomeBase  = GetVector3(aMainBase.pos);
            var posAlignBase = GetVector3(aBaseToAlign.pos);

            var posHomeBaseRotBack = GetMatrix4x4(aMainBase.rot);
            var posHomeBaseRot     = posHomeBaseRotBack.Transpose();

            var posNormAlignBaseTrans = posAlignBase - posHomeBase;
            var posNormAlignBaseRot = Vector3.Transform(posNormAlignBaseTrans, posHomeBaseRot);
            posNormAlignBaseRot = new Vector3(((int)Math.Round(posNormAlignBaseRot.X + 1)) / 2 * 2, 
                                              ((int)Math.Round(posNormAlignBaseRot.Y + 1)) / 2 * 2, 
                                              ((int)Math.Round(posNormAlignBaseRot.Z + 1)) / 2 * 2);
            var posNormAlignBaseRotBack = Vector3.Transform(posNormAlignBaseRot + aShiftVector, posHomeBaseRotBack);
            var posNormAlignBaseRotBackTans = posNormAlignBaseRotBack + posHomeBase;

            return new IdPositionRotation() { id = aBaseToAlign.id, pos = GetVector3(posNormAlignBaseRotBackTans), rot = aMainBase.rot };
        }


    }
}
