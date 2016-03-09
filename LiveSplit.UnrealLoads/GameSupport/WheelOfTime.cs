﻿using LiveSplit.ComponentUtil;
using LiveSplit.Model.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace LiveSplit.UnrealLoads.GameSupport
{
	class WheelOfTime : IGameSupport
	{
		public HashSet<string> GameNames { get; protected set; } = new HashSet<string>
		{
			"Wheel of Time",
			"wot"
		};

		public HashSet<string> ProcessNames { get; protected set; } = new HashSet<string>
		{
			"wot"
		};

		public HashSet<string> Maps { get; protected set; } = new HashSet<string>
		{
			"mission_02",
			"mission_03",
			"mission_04",
			"mission_05a",
			"mission_05b",
			"mission_05c",
			"mission_07a",
			"mission_07b",
			"mission_08a",
			"mission_08b",
			"mission_08c",
			"mission_10",
			"mission_12a",
			"mission_12b",
			"mission_15",
			"mission_16",
			"mission_17",
			"mission_18",
			"mission_13"
		};

		List<KeyOrButton>	_hideMissionObjectivesKeys;
		CompositeHook		_hook;
		Process				_game;
		StringWatcher		_map;
		bool				_shouldStart;

		public WheelOfTime()
		{
			_hideMissionObjectivesKeys = new List<KeyOrButton>();
			_hook = new CompositeHook();
			_hook.KeyOrButtonPressed += hook_KeyOrButtonPressed;
		}

		public TimerAction[] OnUpdate(Process game, MemoryWatcherList watchers)
		{
			if (_shouldStart)
			{
				_shouldStart = false;
				return new TimerAction[] { TimerAction.Start };
			}

			return null;
		}

		public TimerAction[] OnMapLoad(StringWatcher map)
		{
			_map = map;
			if (map.Current.ToLower() == "mission_01")
				return new TimerAction[] { TimerAction.Reset };

			return null;
		}

		public TimerAction[] OnAttach(Process game)
		{
			_game = game;
			ParseInis(game);
			return null;
		}

		public TimerAction[] OnDetach(Process game)
		{
			_hook.UnregisterAllHotkeys();
			return null;
		}

		void ParseInis(Process game)
		{
			string path = game.MainModule.FileName;
			path = path.Remove(path.LastIndexOf(game.ProcessName + ".exe"), (game.ProcessName + ".exe").Length);
			string pathUserIni = path + "User.ini";

			Debug.WriteLine("[NoLoads] Path to inis: User.ini: \"" + pathUserIni + "\"");
			foreach (string line in System.IO.File.ReadAllLines(pathUserIni))
			{
				Match regex = Regex.Match(line, @"([^ ?]+)=(?:ShowMissionObjectives|ShowMenu)");
				if (regex.Success)
					_hideMissionObjectivesKeys.Add(new KeyOrButton(regex.Groups[1].Value));
			}

			_hook.UnregisterAllHotkeys();
			foreach (KeyOrButton key in _hideMissionObjectivesKeys)
			{
				_hook.RegisterHotKey(key);
			}
		}

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		void hook_KeyOrButtonPressed(object sender, KeyOrButton e)
		{
			if (_game == null || _game.HasExited || _map == null || _game.MainWindowHandle != GetForegroundWindow())
				return;

			foreach (KeyOrButton key in _hideMissionObjectivesKeys)
			{
				if (key == e && _map.Current.ToLower() == "mission_01")
				{
					_shouldStart = true;
					break;
				}
			}
		}

		public IdentificationResult IdentifyProcess(Process process) => IdentificationResult.Success;
		public bool? IsLoading(MemoryWatcherList watchers) => null;
	}
}
