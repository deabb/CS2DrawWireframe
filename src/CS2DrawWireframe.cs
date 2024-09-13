/*
Copyright (C) 2024 Deana Brcka
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text.Json;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace CS2DrawWireframe
{
    [MinimumApiVersion(247)]
    public partial class CS2DrawWireframe : BasePlugin
    {
        public override string ModuleName => "CS2DrawWireframe";
        public override string ModuleVersion => "1.2";
        public override string ModuleAuthor => "Deana https://x.com/dea_bb/";
        public override string ModuleDescription => "A Plugin that draws and removes wireframe models from a json file.";

        public class WireframeModel
        {
            public List<string> Vertices { get; set; } = [];
            public List<int[]> Edges { get; set; } = [];
        }

        public class BeamManager
        {
            private List<CBeam> beams = [];

            public void AddBeam(CBeam beam)
            {
                beams.Add(beam);
            }

            public void RemoveAllBeams()
            {
                foreach (CBeam beam in beams)
                {
                    beam.Remove();
                }
                beams.Clear();
            }
        }

        private string wireframeFilePath = "wireframe.json";
        private DateTime lastFileWriteTime;
        private BeamManager beamManager = new();
        private Vector? globalOffset = null;
        private bool timerShouldDoTimerStuff = false;

        public override void Load(bool hotReload)
        {
            string gameDir = Server.GameDirectory;
            string wireframeFileName = "CS2DrawWireframe/wireframe.json";
            wireframeFilePath = Path.Join(gameDir + "/csgo/cfg", wireframeFileName);

            AddTimer(0.05f, () =>
            {
                if (timerShouldDoTimerStuff && globalOffset != null)
                {
                    DateTime currentWriteTime = File.GetLastWriteTime(wireframeFilePath);
        
                    if (currentWriteTime > lastFileWriteTime)
                    {
                        PrintDebug("File has changed, updating wireframe...");
                        beamManager.RemoveAllBeams();
                        lastFileWriteTime = currentWriteTime;
        
                        _ = Task.Run(async () =>
                        {
                            WireframeModel wireframe = await LoadWireframeFromFile(wireframeFilePath);
        
                            if (wireframe.Vertices.Count == 0 || wireframe.Edges.Count == 0)
                            {
                                PrintDebug("No valid vertices or edges in the updated JSON file.");
                                return;
                            }
        
                            globalOffset ??= new Vector(0, 0, 0);
                            DrawWireframe([.. wireframe.Vertices], wireframe.Edges, $"{globalOffset.X} {globalOffset.Y} {globalOffset.Z}");
                        });
                    }
                }
            }, TimerFlags.REPEAT);

            PrintDebug($"Plugin loaded");
        }

        public override void Unload(bool hotReload)
        {
            beamManager.RemoveAllBeams();
        }

        public async Task<WireframeModel> LoadWireframeFromFile(string filePath)
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(filePath);
                PrintDebug($"Read JSON file from {filePath}");
                return JsonSerializer.Deserialize<WireframeModel>(jsonContent)!;
            }
            catch (Exception ex)
            {
                PrintDebug($"Error reading or parsing JSON file: {ex.Message}");
                return new WireframeModel();
            }
        }

        public void DrawWireframe(string[] vertices, List<int[]> edges, string offset = "0 0 0")
        {
            foreach (var edge in edges)
            {
                if (edge.Length == 2)
                {
                    Server.NextFrame(() =>
                    {
                        Vector startVertex = ParseVector(vertices[edge[0]]);
                        Vector endVertex = ParseVector(vertices[edge[1]]);
                        Vector offsetVector = ParseVector(offset);
                        DrawLine(startVertex, endVertex, offsetVector);
                    });
                }
            }
        }

        public void DrawLine(Vector startPos, Vector endPos, Vector? offset = null)
        {
            if (offset != null)
            {
                startPos.X += offset.X;
                startPos.Y += offset.Y;
                startPos.Z += offset.Z;

                endPos.X += offset.X;
                endPos.Y += offset.Y;
                endPos.Z += offset.Z;
            }

            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;
            if (beam == null)
            {
                PrintDebug($"Failed to create beam...");
                return;
            }

            beam.Render = Color.FromName("white");
            beam.Width = 0.1f;

            beam.Teleport(startPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));

            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;

            beam.DispatchSpawn();
            beamManager.AddBeam(beam);
            PrintDebug($"Beam Spawned at S:{startPos} E:{beam.EndPos}");
        }

        [ConsoleCommand("css_draw", "Draws a wireframe model from a json file.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CreateWireframeAtPlayer(CCSPlayerController? player, CommandInfo command)
        {
            beamManager.RemoveAllBeams();
            if (globalOffset == null) globalOffset = new Vector(player?.PlayerPawn?.Value!.AbsOrigin!.X, player?.PlayerPawn?.Value!.AbsOrigin!.Y, player?.PlayerPawn?.Value!.AbsOrigin!.Z);

            _ = Task.Run(async () =>
            {
                WireframeModel wireframe = await LoadWireframeFromFile(wireframeFilePath);

                if (wireframe.Vertices.Count == 0 || wireframe.Edges.Count == 0)
                {
                    PrintDebug("No valid vertices or edges in the JSON file.");
                    return;
                }

                DrawWireframe([.. wireframe.Vertices], wireframe.Edges, $"{globalOffset.X} {globalOffset.Y} {globalOffset.Z}");
                timerShouldDoTimerStuff = true;
            });
        }

        [ConsoleCommand("css_remove", "Removes all beams created by the wireframe.")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RemoveAllBeams(CCSPlayerController? player, CommandInfo command)
        {
            beamManager.RemoveAllBeams();
            timerShouldDoTimerStuff = false;
            globalOffset = null;
            PrintDebug("All beams removed.");
        }
    }
}