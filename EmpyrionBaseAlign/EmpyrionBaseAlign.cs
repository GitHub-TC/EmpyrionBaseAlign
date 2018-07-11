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
        public IdPositionRotation BaseToAlign { get; private set; }
        public IdPositionRotation MainBase { get; private set; }
        public bool WithinAlign { get; private set; }

        public Dictionary<int, IdPositionRotation> OriginalPosRot { get; set; } = new Dictionary<int, IdPositionRotation>();

        class LastAlignData
        {
            public int PlayerId;
            public int BaseToAlignId;
            public int MainBaseId;
            public Vector3 ShiftVector;
            public Vector3 RotateVector;
        }

        Dictionary<int, LastAlignData> PlayerLastAlignData { get; set; } = new Dictionary<int, LastAlignData>();

        LastAlignData CurrentAlignData;

        enum SubCommand
        {
            Help,
            Align,
            Shift,
            Rotate,
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;
            this.verbose = true;

            this.log($"**HandleEmpyrionBaseAlign loaded");

            Event_Entity_PosAndRot += EmpyrionBaseAlign_Event_Entity_PosAndRot;

            this.ChatCommands.Add(new ChatCommand(@"/al", (C, A) => ExecAlignCommand(SubCommand.Help, C, A), "Hilfe anzeigen"));
            this.ChatCommands.Add(new ChatCommand(@"/al (?<BaseToAlignId>\d+) (?<MainBaseId>\d+)",   (C, A) => ExecAlignCommand(SubCommand.Align, C, A), "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten"));

            this.ChatCommands.Add(new ChatCommand(@"/als (?<ShiftX>.+),(?<ShiftY>.+),(?<ShiftZ>.+)", (C, A) => ExecAlignCommand(SubCommand.Shift, C, A), "Letzte /al {BaseToAlignId} um {ShiftX},{ShiftY},{ShiftZ} verschieben"));
            this.ChatCommands.Add(new ChatCommand(@"/alr (?<RotateX>.+),(?<RotateY>.+),(?<RotateZ>.+)",       (C, A) => ExecAlignCommand(SubCommand.Rotate, C, A), "Letzte /al {BaseToAlignId} um {RotateX},{RotateY},{RotateZ} drehen"));
        }

        private void EmpyrionBaseAlign_Event_Entity_PosAndRot(IdPositionRotation aData)
        {
            if (aData.id == CurrentAlignData.MainBaseId) MainBase    = aData;
            if (aData.id == CurrentAlignData.BaseToAlignId) BaseToAlign = aData;

            if ((MainBase == null && CurrentAlignData.MainBaseId != 0) || BaseToAlign == null || WithinAlign) return;
            WithinAlign = true;

            if (!OriginalPosRot.ContainsKey(BaseToAlign.id)) OriginalPosRot.Add(BaseToAlign.id, BaseToAlign);

            var AlignResult = BaseToAlign = OriginalPosRot[BaseToAlign.id];
            
            if (CurrentAlignData.MainBaseId != 0)
            {
                this.log($"**HandleEmpyrionBaseAlign:ExecAlign {MainBase.id} pos= {MainBase.pos.x},{MainBase.pos.y},{MainBase.pos.z} rot= {MainBase.rot.x},{MainBase.rot.y},{MainBase.rot.z} Align: {BaseToAlign.id} pos= {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} rot= {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} Shift={CurrentAlignData.ShiftVector.X},{CurrentAlignData.ShiftVector.Y},{CurrentAlignData.ShiftVector.Z}  Rotate={CurrentAlignData.RotateVector.X},{CurrentAlignData.RotateVector.Y},{CurrentAlignData.RotateVector.Z}");

                PlayerLastAlignData[CurrentAlignData.PlayerId] = CurrentAlignData;

                AlignResult = ExecAlign(MainBase, BaseToAlign, CurrentAlignData.ShiftVector, CurrentAlignData.RotateVector);
            }

            this.log($"**HandleEmpyrionBaseAlign:Align {(CurrentAlignData.MainBaseId == 0 ? "UNDO" : "")} setposition {BaseToAlign.id} {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} setrotation {BaseToAlign.id} {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} -> \n" +
                     $"setposition {BaseToAlign.id} {AlignResult.pos.x},{AlignResult.pos.y},{AlignResult.pos.z} setrotation {BaseToAlign.id} {AlignResult.rot.x},{AlignResult.rot.y},{AlignResult.rot.z}");
            GameAPI.Game_Request(CmdId.Request_Entity_Teleport, 1, AlignResult);
            WithinAlign = false;
        }

        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private void ExecAlignCommand(SubCommand aSubCommand, ChatInfo info, Dictionary<string, string> args)
        {
            this.log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}");

            if (info.type != (byte)ChatType.Faction) return;

            if(!PlayerLastAlignData.TryGetValue(info.playerId, out CurrentAlignData)) PlayerLastAlignData.Add(info.playerId, CurrentAlignData = new LastAlignData() { PlayerId = info.playerId });

            switch (aSubCommand)
            {
                case SubCommand.Help  : DisplayHelp(info); return;
                case SubCommand.Align : CurrentAlignData.BaseToAlignId = getIntParam(args, "BaseToAlignId");
                                        CurrentAlignData.MainBaseId    = getIntParam(args, "MainBaseId");
                                        break;
                case SubCommand.Shift : CurrentAlignData.ShiftVector  += new Vector3(getIntParam(args, "ShiftX"), getIntParam(args, "ShiftY"), getIntParam(args, "ShiftZ"));
                                        break;
                case SubCommand.Rotate: CurrentAlignData.RotateVector += new Vector3(getIntParam(args, "RotateX"), getIntParam(args, "RotateY"), getIntParam(args, "RotateZ"));
                                        break;
            }

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
                    this.Request_Structure_BlockStatistics(new Id(CurrentAlignData.BaseToAlignId), O =>
                    {
                        if (O.blockStatistics.Aggregate(0, (s, i) => s + i.Value) > 10) this.log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {O.blockStatistics.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")} -> zu viele Blöcke");
                        else                                                            GetPosAndRotThenExecAlign();
                    });
            });
        }

        private void GetPosAndRotThenExecAlign()
        {
            GetEntity_PosAndRot(CurrentAlignData.BaseToAlignId);
            if(CurrentAlignData.MainBaseId != 0) GetEntity_PosAndRot(CurrentAlignData.MainBaseId);
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

        public static IdPositionRotation ExecAlign(IdPositionRotation aMainBase, IdPositionRotation aBaseToAlign, Vector3 aShiftVector, Vector3 aRotateVector)
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

            return new IdPositionRotation() { id = aBaseToAlign.id, pos = GetVector3(posNormAlignBaseRotBackTans), rot = GetVector3(GetVector3(aMainBase.rot) + aRotateVector) };
        }


    }
}
