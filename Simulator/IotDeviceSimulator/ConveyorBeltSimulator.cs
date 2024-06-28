namespace IotDeviceSimulator
{
    public class ConveyorBeltSimulator
    {
        Random rand = new Random();

        private readonly int intervalInSeconds;

        // Conveyor belt globals.
        public enum SpeedEnum
        {
            stopped,
            slow,
            fast
        }
        // Count of packages leaving the conveyor belt.
        private int packageCount = 0;
        // Initial state of the conveyor belt.
        private SpeedEnum beltSpeed = SpeedEnum.stopped;
        // Packages completed at slow speed/ per second
        private readonly double slowPackagesPerSecond = 1;
        // Packages completed at fast speed/ per second
        private readonly double fastPackagesPerSecond = 2;
        // Time the belt has been stopped.
        private double beltStoppedSeconds = 0;
        // Ambient temperature of the facility.
        private double temperature = 60;
        // Time conveyor belt is running.
        private double seconds = 0;

        // Vibration globals.
        // Time since forced vibration started.
        private double forcedSeconds = 0;
        // Time since increasing vibration started.
        private double increasingSeconds = 0;
        // Constant identifying the severity of natural vibration.
        private double naturalConstant;
        // Constant identifying the severity of forced vibration.
        private double forcedConstant = 0;
        // Constant identifying the severity of increasing vibration.
        private double increasingConstant = 0;

        public double BeltStoppedSeconds { get => beltStoppedSeconds; }
        public int PackageCount { get => packageCount; }
        public double Temperature { get => temperature; }
        public SpeedEnum BeltSpeed { get => beltSpeed; }

        public ConveyorBeltSimulator(int intervalInMilliseconds)
        {
            // Create a number between 2 and 4, as a constant for normal vibration levels.
            naturalConstant = 2 + 2 * rand.NextDouble();
            // Time interval in seconds.
            intervalInSeconds = intervalInMilliseconds / 1000;
        }

        internal double ReadVibration()
        {
            double vibration;

            // Randomly adjust belt speed.
            switch (beltSpeed)
            {
                case SpeedEnum.fast:
                    if (rand.NextDouble() < 0.01)
                    {
                        beltSpeed = SpeedEnum.stopped;
                    }
                    if (rand.NextDouble() > 0.95)
                    {
                        beltSpeed = SpeedEnum.slow;
                    }
                    break;

                case SpeedEnum.slow:
                    if (rand.NextDouble() < 0.01)
                    {
                        beltSpeed = SpeedEnum.stopped;
                    }
                    if (rand.NextDouble() > 0.95)
                    {
                        beltSpeed = SpeedEnum.fast;
                    }
                    break;

                case SpeedEnum.stopped:
                    if (rand.NextDouble() > 0.75)
                    {
                        beltSpeed = SpeedEnum.slow;
                    }
                    break;
            }

            // Set vibration levels.
            if (beltSpeed == SpeedEnum.stopped)
            {
                // If the belt is stopped, all vibration comes to a halt.
                forcedConstant = 0;
                increasingConstant = 0;
                vibration = 0;

                // Record how much time the belt is stopped, in case we need to send an alert.
                beltStoppedSeconds += intervalInSeconds;
            }
            else
            {
                // Conveyor belt is running.
                beltStoppedSeconds = 0;

                // Check for random starts in unwanted vibrations.

                // Check forced vibration.
                if (forcedConstant == 0)
                {
                    if (rand.NextDouble() < 0.1)
                    {
                        // Forced vibration starts.
                        // A number between 1 and 7.
                        forcedConstant = 1 + 6 * rand.NextDouble();
                        if (beltSpeed == SpeedEnum.slow)
                        {
                            // Lesser vibration if slower speeds.
                            forcedConstant /= 2;
                        }
                        // Lesser vibration if slower speeds.
                        forcedSeconds = 0;
                        ConsoleHelper.WriteRedMessage($"Forced vibration starting with severity: {Math.Round(forcedConstant, 2)}");
                    }
                }
                else
                {
                    if (rand.NextDouble() > 0.99)
                    {
                        forcedConstant = 0;
                        ConsoleHelper.WriteGreenMessage("Forced vibration stopped");
                    }
                    else
                    {
                        ConsoleHelper.WriteRedMessage($"Forced vibration: {Math.Round(forcedConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                    }
                }

                // Check increasing vibration.
                if (increasingConstant == 0)
                {
                    if (rand.NextDouble() < 0.05)
                    {
                        // Increasing vibration starts.
                        // A number between 100 and 200.
                        increasingConstant = 100 + 100 * rand.NextDouble();
                        if (beltSpeed == SpeedEnum.slow)
                        {
                            // Longer period if slower speeds.
                            increasingConstant *= 2;
                        }
                        increasingSeconds = 0;
                        ConsoleHelper.WriteRedMessage($"Increasing vibration starting with severity: {Math.Round(increasingConstant, 2)}");
                    }
                }
                else
                {
                    if (rand.NextDouble() > 0.99)
                    {
                        increasingConstant = 0;
                        ConsoleHelper.WriteGreenMessage("Increasing vibration stopped");
                    }
                    else
                    {
                        ConsoleHelper.WriteRedMessage($"Increasing vibration: {Math.Round(increasingConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                    }
                }

                // Apply the vibrations, starting with natural vibration.
                vibration = naturalConstant * Math.Sin(seconds);

                if (forcedConstant > 0)
                {
                    // Add forced vibration.
                    vibration += forcedConstant * Math.Sin(0.75 * forcedSeconds) * Math.Sin(10 * forcedSeconds);
                    forcedSeconds += intervalInSeconds;
                }

                if (increasingConstant > 0)
                {
                    // Add increasing vibration.
                    vibration += (increasingSeconds / increasingConstant) * Math.Sin(increasingSeconds);
                    increasingSeconds += intervalInSeconds;
                }
            }

            // Increment the time since the conveyor belt app started.
            seconds += intervalInSeconds;

            // Count the packages that have completed their journey.
            switch (beltSpeed)
            {
                case SpeedEnum.fast:
                    packageCount += (int)(fastPackagesPerSecond * intervalInSeconds);
                    break;

                case SpeedEnum.slow:
                    packageCount += (int)(slowPackagesPerSecond * intervalInSeconds);
                    break;

                case SpeedEnum.stopped:
                    // No packages!
                    break;
            }

            // Randomly vary ambient temperature.
            temperature += rand.NextDouble() - 0.5d;
            return vibration;
        }
    }
}
