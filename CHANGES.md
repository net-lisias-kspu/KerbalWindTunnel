# Kerbal Wind Tunnel :: Changes

* 2018-0828: 1.1 (Booots) for KSP 1.4.5
	+ Performance improvements, mostly.
		- Adds the Accord.Net library (redistributed under its own license) for improved robustness.
		- Calculates max AoA now, using Accord.Net's optimization toolbox.
		- Switched to using a custom ThreadPool for better management and slightly improved speeds.
		- Graphs a coarse view first and then continue refining it in the background.
		- Caches the flight envelope data between planet changes instead of recalculating.
		- Intuitively clears the graphs and resets them when creating a new vessel or loading one.
	+ More graph options are implemented in the back end, but aren't yet available in the GUI.
