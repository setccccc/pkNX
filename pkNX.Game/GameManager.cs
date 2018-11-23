﻿using System;
using System.IO;
using pkNX.Containers;
using pkNX.Structures;

namespace pkNX.Game
{
    /// <summary>
    /// Manages fetching of game data.
    /// </summary>
    public sealed class GameManager
    {
        private readonly GameLocation ROM;
        private readonly TextManager Text; // GameText
        private readonly GameFileMapping FileMap;
        public readonly GameInfo Info;

        public string ROMPath => ROM.RomFS;
        public string EXEPath => ROM.ExeFS;

        /// <summary>
        /// Language to use when fetching string &amp; graphic assets.
        /// </summary>
        public int Language { get; set; }

        /// <summary>
        /// Current <see cref="GameVersion"/> the data represents.
        /// </summary>
        public GameVersion Game => ROM.Game;

        /// <summary>
        /// Generally useful game data that can be used by multiple editors.
        /// </summary>
        public GameData Data { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="GameManager"/> for the input <see cref="GameLocation"/> with initial <see cref="Language"/>.
        /// </summary>
        /// <param name="rom"></param>
        /// <param name="language"></param>
        public GameManager(GameLocation rom, int language)
        {
            ROM = rom;
            Language = language;
            FileMap = new GameFileMapping(rom);
            Initialize();
            Text = new TextManager(Game);
            Info = new GameInfo(Game);
        }

        /// <summary>
        /// Fetches a <see cref="GameFile"/> from the Game data.
        /// </summary>
        /// <param name="file">File type to fetch</param>
        /// <returns>Container that contains the game data requested.</returns>
        /// <remarks>Sugar for the other <see cref="GetFile"/> method.</remarks>
        public IFileContainer this[GameFile file] => GetFile(file);

        /// <summary>
        /// Fetches a <see cref="GameFile"/> from the Game data.
        /// </summary>
        /// <param name="file">File type to fetch</param>
        /// <returns>Container that contains the game data requested.</returns>
        public IFileContainer GetFile(GameFile file) => FileMap.GetFile(file, Language);

        /// <summary>
        /// Fetches strings for the input <see cref="TextName"/>.
        /// </summary>
        /// <param name="text">Text file to fetch</param>
        /// <returns>Array of strings from the requested text file.</returns>
        public string[] GetStrings(TextName text)
        {
            var arc = this[GameFile.GameText];
            var lines = Text.GetStrings(text, arc);
            return lines;
        }

        /// <summary>
        /// Saves all open files and finalizes the ROM data.
        /// </summary>
        /// <param name="closing">Skip re-initialization of game data.</param>
        public void SaveAll(bool closing)
        {
            FileMap.SaveAll();
            if (!closing)
                Initialize();
        }

        private void Initialize()
        {
            if (ROM.Game == GameVersion.GG)
                InitializeDataGG();
        }

        private void InitializeDataGG()
        {
            // initialize gametext
            GetFilteredFolder(GameFile.GameText, z => Path.GetExtension(z) == ".dat");

            // initialize common structures
            Data = new GameData
            {
                MoveData = new DataCache<Move>(this[GameFile.MoveStats]) // mini
                {
                    Create = z => new Move7(z),
                    Write = z => z.Write(),
                },
                LevelUpData = new DataCache<Learnset>(this[GameFile.Learnsets]) // gfpak
                {
                    Create = z => new Learnset6(z),
                    Write = z => z.Write(),
                },

                // folders
                PersonalData = new PersonalTable(GetFilteredFolder(GameFile.PersonalStats, z => Path.GetFileNameWithoutExtension(z) == "personal_total").GetFiles().Result[0], Game),
                MegaEvolutionData = new DataCache<MegaEvolutionSet[]>(GetFilteredFolder(GameFile.MegaEvolutions))
                {
                    Create = MegaEvolutionSet.ReadArray,
                    Write = MegaEvolutionSet.WriteArray,
                },
                EvolutionData = new DataCache<EvolutionSet>(GetFilteredFolder(GameFile.Evolutions))
                {
                    Create = (data) => new EvolutionSet7(data),
                    Write = evo => evo.Write(),
                },
            };
        }

        public FolderContainer GetFilteredFolder(GameFile type, Func<string, bool> filter = null)
        {
            var c = (FolderContainer)this[type];
            c.Initialize(filter);
            return c;
        }
    }
}
