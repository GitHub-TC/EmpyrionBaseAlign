using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using EmpyrionNetAPIDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace EmpyrionBaseAlign
{
    public static class Extensions
    {
        public static T GetAttribute<T>(this Assembly aAssembly)
        {
            return aAssembly.GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();
        }

        static Regex GetCommand = new Regex(@"(?<cmd>(\w|\/|\s)+)");

        public static string MsgString(this ChatCommand aCommand)
        {
            var CmdString = GetCommand.Match(aCommand.invocationPattern).Groups["cmd"]?.Value ?? aCommand.invocationPattern;
            return $"[c][ff00ff]{CmdString}[-][/c]{aCommand.paramNames.Aggregate(" ", (S, P) => S + $"<[c][00ff00]{P}[-][/c]> ")}: {aCommand.description}";
        }

    }
    public partial class EmpyrionBaseAlign : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; set; }
        public IdPositionRotation BaseToAlign { get; private set; }
        public IdPositionRotation MainBase { get; private set; }
        public bool WithinAlign { get; private set; }

        public Dictionary<int, IdPositionRotation> OriginalPosRot { get; set; } = new Dictionary<int, IdPositionRotation>();

        public ConfigurationManager<Configuration> Configuration { get; set; }

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

        public EmpyrionBaseAlign()
        {
            EmpyrionConfiguration.ModName = "EmpyrionBaseAlign";
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;

            log($"**HandleEmpyrionBaseAlign loaded", LogLevel.Message);
            LoadConfiguration();
            LogLevel = Configuration.Current.LogLevel;

            Event_Entity_PosAndRot += EmpyrionBaseAlign_Event_Entity_PosAndRot;

            ChatCommands.Add(new ChatCommand(@"\\al", (C, A) => ExecAlignCommand(SubCommand.Help, C, A), "Hilfe anzeigen"));
            ChatCommands.Add(new ChatCommand(@"\\al (?<BaseToAlignId>\d+) (?<MainBaseId>\d+)", (C, A) => ExecAlignCommand(SubCommand.Align, C, A), "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten"));

            ChatCommands.Add(new ChatCommand(@"\\als (?<ShiftX>.+) (?<ShiftY>.+) (?<ShiftZ>.+)", (C, A) => ExecAlignCommand(SubCommand.Shift, C, A), "Letzte /al {BaseToAlignId} um {ShiftX} {ShiftY} {ShiftZ} verschieben"));
            ChatCommands.Add(new ChatCommand(@"\\alr (?<RotateX>.+) (?<RotateY>.+) (?<RotateZ>.+)", (C, A) => ExecAlignCommand(SubCommand.Rotate, C, A), "Letzte /al {BaseToAlignId} um {RotateX} {RotateY} {RotateZ} drehen"));
        }

        private void LoadConfiguration()
        {
            ConfigurationManager<Configuration>.Log = log;
            Configuration = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json")
            };

            Configuration.Load();
            Configuration.Save();
        }

        private void EmpyrionBaseAlign_Event_Entity_PosAndRot(IdPositionRotation aData)
        {
            if (aData.id == CurrentAlignData.MainBaseId   ) MainBase    = aData;
            if (aData.id == CurrentAlignData.BaseToAlignId) BaseToAlign = aData;

            if ((MainBase == null && CurrentAlignData.MainBaseId != 0) || BaseToAlign == null || WithinAlign) return;
            WithinAlign = true;

            if (!OriginalPosRot.ContainsKey(BaseToAlign.id)) OriginalPosRot.Add(BaseToAlign.id, BaseToAlign);

            var AlignResult = BaseToAlign = OriginalPosRot[BaseToAlign.id];
            
            if (CurrentAlignData.MainBaseId != 0)
            {
                log($"**HandleEmpyrionBaseAlign:ExecAlign {MainBase.id} pos= {MainBase.pos.x},{MainBase.pos.y},{MainBase.pos.z} rot= {MainBase.rot.x},{MainBase.rot.y},{MainBase.rot.z} Align: {BaseToAlign.id} pos= {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} rot= {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} Shift={CurrentAlignData.ShiftVector.X},{CurrentAlignData.ShiftVector.Y},{CurrentAlignData.ShiftVector.Z}  Rotate={CurrentAlignData.RotateVector.X},{CurrentAlignData.RotateVector.Y},{CurrentAlignData.RotateVector.Z}", LogLevel.Message);

                PlayerLastAlignData[CurrentAlignData.PlayerId] = CurrentAlignData;

                AlignResult = ExecAlign(MainBase, BaseToAlign, CurrentAlignData.ShiftVector, CurrentAlignData.RotateVector);
            }

            log($"**HandleEmpyrionBaseAlign:Align {(CurrentAlignData.MainBaseId == 0 ? "UNDO" : "")} setposition {BaseToAlign.id} {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} setrotation {BaseToAlign.id} {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} -> \n" +
                     $"setposition {BaseToAlign.id} {AlignResult.pos.x},{AlignResult.pos.y},{AlignResult.pos.z} setrotation {BaseToAlign.id} {AlignResult.rot.x},{AlignResult.rot.y},{AlignResult.rot.z}", LogLevel.Message);
            GameAPI.Game_Request(CmdId.Request_Entity_Teleport, 1, AlignResult);
            WithinAlign = false;
        }

        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private async Task ExecAlignCommand(SubCommand aSubCommand, ChatInfo info, Dictionary<string, string> args)
        {
            log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

            if (info.type != (byte)ChatType.Faction) return;

            if(!PlayerLastAlignData.TryGetValue(info.playerId, out CurrentAlignData)) PlayerLastAlignData.Add(info.playerId, CurrentAlignData = new LastAlignData() { PlayerId = info.playerId });

            switch (aSubCommand)
            {
                case SubCommand.Help  : await DisplayHelp(info.playerId); return;
                case SubCommand.Align : CurrentAlignData.BaseToAlignId = getIntParam(args, "BaseToAlignId");
                                        CurrentAlignData.MainBaseId    = getIntParam(args, "MainBaseId");
                                        CurrentAlignData.ShiftVector   = Vector3.Zero;
                                        CurrentAlignData.RotateVector  = Vector3.Zero;
                                        break;
                case SubCommand.Shift : CurrentAlignData.ShiftVector  += new Vector3(getIntParam(args, "ShiftX"), getIntParam(args, "ShiftY"), getIntParam(args, "ShiftZ"));
                                        break;
                case SubCommand.Rotate: CurrentAlignData.RotateVector += new Vector3(getIntParam(args, "RotateX"), getIntParam(args, "RotateY"), getIntParam(args, "RotateZ"));
                                        break;
            }

            MainBase = BaseToAlign = null;
            WithinAlign = false;

            await CheckPlayerPermissionThenExecAlign(info);
        }

        private async Task CheckPlayerPermissionThenExecAlign(ChatInfo info)
        {
            var G = await Request_GlobalStructure_List();
            var I = await Request_Player_Info(info.playerId.ToId());

            var playerPermissionLevel = (PermissionType)I.permission;

            if (playerPermissionLevel >= Configuration.Current.FreePermissionLevel) GetPosAndRotThenExecAlign();
            else if(Configuration.Current.ForbiddenPlayfields.Contains(I.playfield)) InformPlayer(info.playerId, $"BaseAlign: Playfield ist verboten");
            else
            {
                var StructureInfoA = SearchEntity(G, CurrentAlignData.BaseToAlignId);
                var StructureInfoB = SearchEntity(G, CurrentAlignData.MainBaseId);
                if (StructureInfoA.factionId == I.factionId && StructureInfoB.factionId == I.factionId) GetPosAndRotThenExecAlign();
                else InformPlayer(info.playerId, $"BaseAlign: Basen stehen nicht beide auf der Fraktion des Spielers");
            }
        }

        public static GlobalStructureInfo SearchEntity(GlobalStructureList aGlobalStructureList, int aSourceId)
        {
            foreach (var TestPlayfieldEntites in aGlobalStructureList.globalStructures)
            {
                var FoundEntity = TestPlayfieldEntites.Value.FirstOrDefault(E => E.id == aSourceId);
                if (FoundEntity.id != 0) return FoundEntity;
            }
            return new GlobalStructureInfo();
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

        private async Task DisplayHelp(int aPlayerId)
        {
            await DisplayHelp(aPlayerId,
                Configuration.Current.ForbiddenPlayfields?.Aggregate("[c][00ffff]ForbiddenPlayfields:[-][/c]", (s, p) => s + $"\n {p}")
            );
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

        public static Matrix4x4 GetMatrix4x4(Vector3 aVector)
        {
            return Matrix4x4.CreateFromYawPitchRoll(
                aVector.Y.ToRadians(),
                aVector.X.ToRadians(),
                aVector.Z.ToRadians());
        }

        public static IdPositionRotation ExecAlign(IdPositionRotation aMainBase, IdPositionRotation aBaseToAlign, Vector3 aShiftVector, Vector3 aRotateVector)
        {
            var posHomeBase  = GetVector3(aMainBase.pos);
            var posAlignBase = GetVector3(aBaseToAlign.pos);

            var posHomeBaseRotBack = GetMatrix4x4(GetVector3(aMainBase.rot));
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
