CSL Service Reserve 
v1.6.0_f4_Build01

Purpose
----------
If you've come up against the maximum of 16384 active vehicles at anyone moment in time limit and your basically staying pegged there enough that some of your city services are not functioning as well as they should because some of them can't get spawned in a timely manner then this mod is for you. It will let you setup a small reserve amount that key city service types can pull from to spawn on thier first try. A small reserve goes a long way. This mod let you set a lower limit for "normal" requests, and let your critical ones though to use the reserved amount between that limit and the real maximum.

It's niche scenario probably but i've seen enough cases and questions about it where I thought this
might me helpful to share.

Transfer reasons to VehicleManger.CreateVehicle() that are allowed to use the reversed amount:
TransferReason.Fire
TransferReason.Fire2
TransferReason.Sick
TransferReason.Sick2
TransferReason.SickMove
TransferReason.Garbage
TransferReason.Dead
TransferReason.Crime
TransferReason.DeadMove
TransferReason.GarbageMove
TransferReason.CriminalMove
TransferReason.Bus
TransferReason.MetroTrain
TransferReason.PassengerTrain
TransferReason.Taxi
TransferReason.Tram
TransferReason.Snow
TransferReason.SnowMove
TransferReason.RoadMaintenance
TransferReason.Flooding
TransferReason.EvacuateVIPA
TransferReason.EvacuateVIPB
TransferReason.EvacuateVIPC
TransferReason.EvacuateVIPD

 

Acknowledgements
-----------------
This mod makes use of Sebastian Schöner's CitiesSkylinesDetour code, and would not be possible
without it. You can find his great project here: https://github.com/sschoener/cities-skylines-detour


Installation\Configuration Information
--------------
Configuation File Location: %SkylinesInstallFolder%\CSLServiceReserve_Config.xml

Where %SkylinesInstallFolder% is the root of your Cities Skylines installation folder, for most that would be something like c:\Games\Steam\steamapps\common\Cities_Skylines .



Configuration File Options
--------------------------

  <DebugLogging>false</DebugLogging>
This enables or disables debug logging. You probably don't need this unless you are having a problem. If you are having a problem you can turn this on via the Options setting in the game, or set it to 'true' here in the config file and reload. 


  <DebugLoggingLevel>0</DebugLoggingLevel>
This controls the level of detail. Debugging set to true and this to '0' is the first level of detail. Setting this past '0' or '1' for most of you will not be needed. Setting it to level '2' will record almost everything it does and start filling up your log fast. Level 3 is developer level only and is not meant to be on for more then a couple minutes. Valid values are integers between 0 and 3 - values are basically ignored if DebugLogging is set to 'false'. 


  <VehicleReserveAmount>6</VehicleReserveAmount>
This is the amount of vehicles you want to reserve, the only time you need to set this here in the config file is if you want a custom amount, ie you the 8,16,24,etc options in the gui are not cutting it for you. Valid values are integers between 2 and 512. If you enter values <> than these the mod will use default settings. 


  <VehicleReserveAmountIndex>8</VehicleReserveAmountIndex>
You can ignore this, it's basically the selected index in the gui of the Reserve vehicle amount you have chosen. It's just stored here. Though it should correct itself automatically if you set a custom VehicleReserveAmount then you can\should set this to 8 while you are at it. Values 0 - 8 are valid.


  <EnableGui>true</EnableGui>
This option control if the CTRL S+V GUI is available during a map\game. It can be controlled by the options setting panel in the game. You can not change this option while a map is loaded. Valid values are 'true|false' Default is true. It's highly recommended but technically not required for the mod to work.
 
  <UseAutoRefresh>true</UseAutoRefresh>
Stores your last setting in-game in the gui for if you had Auto Refresh of the statistics enabled. Valid values are 'true|false' default is true.


  <AutoRefreshSeconds>2</AutoRefreshSeconds>
This is how fast you want the statistics counters to refresh their data in the GUI. Note these are the lower lines, the first line (the vehicle counter is set seperately) Valid values are floating point number between 0.500 and 60.0, the default is 3 seconds.


  <GuiOpacity>0.9</GuiOpacity>
This controls the opacity\transparency of the GUI panel itself. The default is 0.9 or 90%.  Value values are floating point numbers between 0.10 (10%) and 1.0 (100%). Higher equals less transparent.  


 <DumpStatsOnMapEnd>false</DumpStatsOnMapEnd>
This controls if you want to have the mod log basic vehicle statistic to either your normal log or a custom log at the end of every map. Valid values are 'true|false'. Default is false and can be set in the GUI. 


  <ResetStatsEveryXMinutesEnabled>true</ResetStatsEveryXMinutesEnabled>
This controls if you want the GUI stats window in the game to reset it's counters to zero every so often.
This is useful if you're monitoring the situation and want to know how often certain things happen, such as
the fail counters or how many reserves have been used. It's more helpful to know xx over xx period then
xx since you've started the game, or manually cleared. things. Valid values are 'true|false'  Default is false.

  <ResetStatsEveryXMin>20</ResetStatsEveryXMin>
This is how often in minues you want the stats to 'AutoReset' to zero. The default is 20 minutes when enabled. Valid values are integers 1 to 1000000

  <RefreshVehicleCounterSeconds>0.18</RefreshVehicleCounterSeconds>
This is how fast the #Vehicle in use and the #ResInUse counters update on screen. The reason this is different from the other data is changes to this data can be hard to spot at time unless it's updated more then once per second.  The default is 0.180 seconds or between 5-6 times per second. The setting is a 'best effort' the timer may not be exact but it will be close. Valid values are floating point numbers between 0.05 (50ms) and 10.0 (10 seconds).  I do not recommend anything lower than 0.100 as it's a bit overkill after that, or higher then 0.500 as any higher and you may betricked into thinking #resinuse are always zero even when you know you're hitting the max reserved. (they get used so fast you can miss the change )


  <UseCustomDumpFile>true</UseCustomDumpFile>
This is a 'true|false' setting to enable\disable the use of a custom file to store the end-of-map vehicle data
dumps to, that is if you have also enabled that feature. Default is false. 


  <DumpStatsFilePath>CSLServiceReserve_DataDump.txt</DumpStatsFilePath>
This is the custom full path to the file you want to use with the afore mentioned UseCustomDumpFile option. You either have to use a full path including file name, or if you just want the file created in your Cities Skylines installation folder root you can just type a file name.  That said, the full path MUST EXIST for this to work, it will not create a folder for you, but it will create the file. So if you set it too 'c:\mydatafolder\mysubfolder\Somefilename.txt then make sure c:\mydatafolder\mysubfolder exists first, though the file does not and will be created if need be.  The file is appended too over time, it is never overwritten. Each dump is about 1k of data.


  <UseCustomLogFile>false</UseCustomLogFile>
This option allows you to tell the mod instead of printing it's normal log data to the standard CSL output_log.txt log file, to dump it's own logging information to a custom file. This really is only useful for debugging purposes or, you don't want logs overwritten on ever game start. If debug logging is disabled it's probably pointless to use this. Default is disabled.


  <CustomLogFilePath>CSLServiceReserve_Log.txt</CustomLogFilePath>
Works exactly like DumpStatFilePath setting only this one is for the CustomLogFile.  Default when in use is the filename above, created in the root of the CSL install folder.


Q: What happens if you delete your config?
Not to worry, if you lose it the mod will just create you a new one with default settings, so long as it can write to the path.  Though you'll need to check your setting again after that. Even if it can't it will probably still function you'll just be forced to use default settings.

Q: Can I move the in-game GUI panel?
Yes you can click-n-drag it around like most game panels.

Q: Can I resize the in-game GUI panel?
Sorry not at this time, I use most of the dialog's real-estate anyway. I know, that probably sucks if your playing on Tablet or something.

Q: What's the in-game GUI panel 'Log Data' button do?
It logs the information you see on your screen to the log file (or the custom dump one if enabled). Unlike the end-of-map dump though pressing this one logs a bunch of other data you might be interested in or if you're having problems might help in the debugging process.
Examples are information about the game itself like version, commandlines uses, paths in use, what mods you have enabled at the molment and a count of how many you have installed. It also dumps debug information about the internal values of parts of the mod, as well as data about your game like certain limited object counters that might be useful, press it and find out.


Q: What mod is this mod incompatiable with?
Any mod that detours or implements their own VehicleManger.CreateVehicle() function. That is the only method this mod touches.

Q: Does this mod effect my save\saved games.
No. This mod does not interact with your saved games, nor does it touch anything that gets saved. If you turn off the mod nothing will happen other then the game will use full max for all vehicle requests again.