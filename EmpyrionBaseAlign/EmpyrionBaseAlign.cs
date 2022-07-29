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
        public Dictionary<int, IdPositionRotation> OriginalPosRot { get; set; } = new Dictionary<int, IdPositionRotation>();

        public ConfigurationManager<Configuration> Configuration { get; set; }

        class LastAlignData
        {
            public int PlayerId { get; set; }
            public int BaseToAlignId { get; set; }
            public int MainBaseId { get; set; }
            public Vector3 ShiftVector { get; set; }
            public Vector3 RotateVector { get; set; }
            public GlobalStructureInfo BaseToAlignStructureInfo { get; set; }
            public GlobalStructureInfo MainBaseStructureInfo { get; set; }
            public IdPositionRotation BaseToAlignPosAndRot { get; set; }
            public IdPositionRotation MainBasePosAndRot { get; internal set; }
        }

        Dictionary<int, LastAlignData> PlayerLastAlignData { get; set; } = new Dictionary<int, LastAlignData>();

        LastAlignData CurrentAlignData;

        enum SubCommand
        {
            Help,
            Align,
            Shift,
            Rotate,
            Undo,
        }

        public EmpyrionBaseAlign()
        {
            EmpyrionConfiguration.ModName = "EmpyrionBaseAlign";
        }

        public override void Initialize(ModGameAPI aGameAPI)
        {
            GameAPI = aGameAPI;

            Log($"**HandleEmpyrionBaseAlign loaded", LogLevel.Message);
            LoadConfiguration();
            LogLevel = Configuration.Current.LogLevel;
            ChatCommandManager.CommandPrefix = Configuration.Current.CommandPrefix;

            ChatCommands.Add(new ChatCommand(@"al help",                                            (C, A) => ExecAlignCommand(SubCommand.Help, C, A), "Hilfe anzeigen"));
            ChatCommands.Add(new ChatCommand(@"al (?<BaseToAlignId>\d+) (?<MainBaseId>\d+)",        (C, A) => ExecAlignCommand(SubCommand.Align, C, A), "Basis {BaseToAlignId} an Basis {MainBaseId} ausrichten, verschieben und drehen"));
            ChatCommands.Add(new ChatCommand(@"al (?<BaseToAlignId>\d+)",                           (C, A) => ExecAlignCommand(SubCommand.Align, C, A), "Basis {BaseToAlignId} verschieben/drehen"));

            ChatCommands.Add(new ChatCommand(@"al undo",                                            (C, A) => ExecAlignCommand(SubCommand.Undo, C, A), "Basis wieder auf die Ausgangsposition setzen"));

            ChatCommands.Add(new ChatCommand(@"als (?<ShiftX>.+) (?<ShiftY>.+) (?<ShiftZ>.+)",      (C, A) => ExecAlignCommand(SubCommand.Shift, C, A), "Letzte /al {BaseToAlignId} um {ShiftX} {ShiftY} {ShiftZ} verschieben"));
            ChatCommands.Add(new ChatCommand(@"alr (?<RotateX>.+) (?<RotateY>.+) (?<RotateZ>.+)",   (C, A) => ExecAlignCommand(SubCommand.Rotate, C, A), "Letzte /al {BaseToAlignId} um {RotateX} {RotateY} {RotateZ} drehen"));
        }

        private void LoadConfiguration()
        {
            ConfigurationManager<Configuration>.Log = Log;
            Configuration = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json")
            };

            Configuration.Load();
            Configuration.Save();
        }

        enum ChatType
        {
            Global  = 3,
            Faction = 5,
        }

        private async Task ExecAlignCommand(SubCommand aSubCommand, ChatInfo info, Dictionary<string, string> args)
        {
            Log($"**HandleEmpyrionBaseAlign {info.type}:{info.msg} {args.Aggregate("", (s, i) => s + i.Key + "/" + i.Value + " ")}", LogLevel.Message);

            if (info.type != (byte)ChatType.Faction) return;

            if(!PlayerLastAlignData.TryGetValue(info.playerId, out CurrentAlignData)) PlayerLastAlignData.Add(info.playerId, CurrentAlignData = new LastAlignData() { PlayerId = info.playerId });
            var P = await Request_Player_Info(info.playerId.ToId());

            switch (aSubCommand)
            {
                case SubCommand.Help  : await DisplayHelp(info.playerId); return;
                case SubCommand.Align : CurrentAlignData.BaseToAlignId = getIntParam(args, "BaseToAlignId");
                                        CurrentAlignData.MainBaseId    = getIntParam(args, "MainBaseId", -1);
                                        CurrentAlignData.ShiftVector   = Vector3.Zero;
                                        CurrentAlignData.RotateVector  = Vector3.Zero;

                                        CurrentAlignData.BaseToAlignPosAndRot     = await Request_Entity_PosAndRot(CurrentAlignData.BaseToAlignId.ToId());            
                                        CurrentAlignData.BaseToAlignStructureInfo = await Request_GlobalStructure_Info(CurrentAlignData.BaseToAlignId.ToId());
                                        if (!OriginalPosRot.ContainsKey(CurrentAlignData.BaseToAlignId)) OriginalPosRot.Add(CurrentAlignData.BaseToAlignId, CurrentAlignData.BaseToAlignPosAndRot);

                                        if (CurrentAlignData.MainBaseId == -1)
                                        {
                                            CurrentAlignData.MainBaseId            = CurrentAlignData.BaseToAlignId;
                                            CurrentAlignData.MainBaseStructureInfo = CurrentAlignData.BaseToAlignStructureInfo;
                                            CurrentAlignData.MainBasePosAndRot     = CurrentAlignData.BaseToAlignPosAndRot;
                                        }
                                        else if (CurrentAlignData.MainBaseId != 0){ 
                                            CurrentAlignData.MainBaseStructureInfo = await Request_GlobalStructure_Info(CurrentAlignData.MainBaseId.ToId());
                                            CurrentAlignData.MainBasePosAndRot     = await Request_Entity_PosAndRot(CurrentAlignData.MainBaseId.ToId()); 
                                        }

                                        await CheckPlayerPermissionThenExecAlign(info, CurrentAlignData);
                                        break;
                case SubCommand.Shift : CurrentAlignData.ShiftVector  += new Vector3(getIntParam(args, "ShiftX"), getIntParam(args, "ShiftY"), getIntParam(args, "ShiftZ"));
                                        GetPosAndRotThenExecAlign(P, CurrentAlignData);
                                        break;
                case SubCommand.Rotate: CurrentAlignData.RotateVector += new Vector3(getIntParam(args, "RotateX"), getIntParam(args, "RotateY"), getIntParam(args, "RotateZ"));
                                        GetPosAndRotThenExecAlign(P, CurrentAlignData);
                                        break;
                case SubCommand.Undo:   if(CurrentAlignData.BaseToAlignStructureInfo.id == 0 || CurrentAlignData.MainBaseStructureInfo.id == 0) return;

                                        CurrentAlignData.ShiftVector  = Vector3.Zero;
                                        CurrentAlignData.RotateVector = Vector3.Zero;

                                        await Request_Entity_Teleport(OriginalPosRot[CurrentAlignData.BaseToAlignId]);
                                        break;
            }

        }

        private async Task CheckPlayerPermissionThenExecAlign(ChatInfo info, LastAlignData currentAlignData)
        {
            var P = await Request_Player_Info(info.playerId.ToId());

            if(currentAlignData.BaseToAlignStructureInfo.id == 0)
            {
                InformPlayer(info.playerId, $"BaseAlign: Structure {currentAlignData.BaseToAlignId} not exists.");
                return;
            }

            if (currentAlignData.MainBaseStructureInfo.id == 0)
            {
                InformPlayer(info.playerId, $"BaseAlign: Structure {currentAlignData.MainBaseId} not exists.");
                return;
            }

            var playerPermissionLevel = (PermissionType)P.permission;

            if (playerPermissionLevel >= Configuration.Current.FreePermissionLevel) return;
            else if (Configuration.Current.ForbiddenPlayfields.Contains(P.playfield))
            {
                currentAlignData.BaseToAlignStructureInfo = currentAlignData.MainBaseStructureInfo = new GlobalStructureInfo();
                InformPlayer(info.playerId, $"BaseAlign: Playfield ist verboten");
            }
            else
            {
                if (currentAlignData.BaseToAlignStructureInfo.factionId == P.factionId && currentAlignData.MainBaseStructureInfo.factionId == P.factionId) return;
                else
                {
                    currentAlignData.BaseToAlignStructureInfo = currentAlignData.MainBaseStructureInfo = new GlobalStructureInfo();
                    InformPlayer(info.playerId, $"BaseAlign: Basen stehen nicht beide auf der Fraktion des Spielers");
                }
            }
        }

        private async void GetPosAndRotThenExecAlign(PlayerInfo player, LastAlignData currentAlignData)
        {
            if(currentAlignData.BaseToAlignStructureInfo.id == 0 || currentAlignData.MainBaseStructureInfo.id == 0) return;

            var BaseToAlign = OriginalPosRot[currentAlignData.BaseToAlignId];

            Log($"**HandleEmpyrionBaseAlign:ExecAlign {CurrentAlignData.MainBasePosAndRot.id} pos= {CurrentAlignData.MainBasePosAndRot.pos.x},{CurrentAlignData.MainBasePosAndRot.pos.y},{CurrentAlignData.MainBasePosAndRot.pos.z} rot= {CurrentAlignData.MainBasePosAndRot.rot.x},{CurrentAlignData.MainBasePosAndRot.rot.y},{CurrentAlignData.MainBasePosAndRot.rot.z} Align: {BaseToAlign.id} pos= {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} rot= {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} Shift={CurrentAlignData.ShiftVector.X},{CurrentAlignData.ShiftVector.Y},{CurrentAlignData.ShiftVector.Z}  Rotate={CurrentAlignData.RotateVector.X},{CurrentAlignData.RotateVector.Y},{CurrentAlignData.RotateVector.Z}", LogLevel.Message);

            PlayerLastAlignData[CurrentAlignData.PlayerId] = CurrentAlignData;

            var AlignResult = ExecAlign(CurrentAlignData.MainBasePosAndRot, BaseToAlign, CurrentAlignData.ShiftVector, CurrentAlignData.RotateVector);

            var answer = await ShowDialog(player.entityId, player, "Change Pos/Rotation", $"Change structure '[c][00ff00]{CurrentAlignData.BaseToAlignStructureInfo.name}[-][/c] ({CurrentAlignData.BaseToAlignStructureInfo.id})'\n" +
                $"pos X:{BaseToAlign.pos.x} -> {AlignResult.pos.x}\n" +
                $"pos Y:{BaseToAlign.pos.y} -> {AlignResult.pos.y}\n" +
                $"pos Z:{BaseToAlign.pos.z} -> {AlignResult.pos.z}\n" +
                $"rot X:{BaseToAlign.rot.x} -> {AlignResult.rot.x}\n" +
                $"rot Y:{BaseToAlign.rot.y} -> {AlignResult.rot.y}\n" +
                $"rot Z:{BaseToAlign.rot.z} -> {AlignResult.rot.z}\n",
                "Yes", "No");
            if (answer.Id != player.entityId || answer.Value != 0) return;

            Log($"**HandleEmpyrionBaseAlign:Align setposition {BaseToAlign.id} {BaseToAlign.pos.x},{BaseToAlign.pos.y},{BaseToAlign.pos.z} setrotation {BaseToAlign.id} {BaseToAlign.rot.x},{BaseToAlign.rot.y},{BaseToAlign.rot.z} -> \n" +
                     $"setposition {BaseToAlign.id} {AlignResult.pos.x},{AlignResult.pos.y},{AlignResult.pos.z} setrotation {BaseToAlign.id} {AlignResult.rot.x},{AlignResult.rot.y},{AlignResult.rot.z}", LogLevel.Message);

            await Request_Entity_Teleport(AlignResult);
        }

        private int getIntParam(Dictionary<string, string> aArgs, string aParameterName, int defaultIfNotFound = 0)
        {
            string valueStr;
            if (!aArgs.TryGetValue(aParameterName, out valueStr)) return defaultIfNotFound;

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
