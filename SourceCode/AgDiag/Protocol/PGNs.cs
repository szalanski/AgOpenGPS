namespace AgDiag.Protocol
{
    public class PGNs
    {
        //AutoSteerData
        public class CPGN_FE
        {
            /// <summary>
            /// autoSteerData FE 254 speedHi=5 speedLo=6  status = 7 free = 8;
            /// steerAngleHi = 9 steerAngleLo = 10 tramControl = 11 sc1to8 = 12 sc9to16 = 13;
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xFE, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC };
            public int speedLo = 5;
            public int speedHi = 6;
            public int status = 7;
            public int steerAngleLo = 8;
            public int steerAngleHi = 9;
            //public int  = 10;
            public int sc1to8 = 11;
            public int sc9to16 = 12;
        }

        public class CPGN_FD
        {
            /// <summary>
            /// From steer module
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xFD, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC };
            public int actualLo = 5;
            public int actualHi = 6;
            public int headLo = 7;
            public int headHi = 8;
            public int rollLo = 9;
            public int rollHi = 10;
            public int switchStatus = 11;
            public int pwm = 12;
        }

        //AutoSteer Settings
        public class CPGN_FC
        {
            /// <summary>
            /// PGN - 252 - FC gainProportional=5 HighPWM=6  LowPWM = 7 MinPWM = 8 
            /// CountsPerDegree = 9 wasOffsetHi = 10 wasOffsetLo = 11 
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xFC, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC };
            public int gainProportional = 5;
            public int highPWM = 6;
            public int lowPWM = 7;
            public int minPWM = 8;
            public int countsPerDegree = 9;
            public int wasOffsetLo = 10;
            public int wasOffsetHi = 11;
            public int ackerman = 12;
        }

        //Autosteer Board Config
        public class CPGN_FB
        {
            /// <summary>
            /// 
            /// PGN - 251 - FB 
            /// set0=5 maxPulse = 6 minSpeed = 7 ackermanFix = 8
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xFB, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC };
            public int set0 = 5;
            public int maxPulse = 6;
            public int minSpeed = 7;

            public CPGN_FB()
            {
                pgn[set0] = 0;
                pgn[maxPulse] = 0;
                pgn[minSpeed] = 0;
            }
        }

        //Machine Data
        public class CPGN_EF
        {
            /// <summary>
            /// PGN - 239 - EF 
            /// uturn=5  tree=6  hydLift = 8 
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xEF, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC };
            public int uturn = 5;
            public int tree = 6;
            public int hydLift = 7;
            public int tram = 8;
            public int sc1to8 = 11;
            public int sc9to16 = 12;
        }

        //Machine Config
        public class CPGN_EE
        {
            /// <summary>
            /// PGN - 238 - EE 
            /// raiseTime=5  lowerTime=6   enableHyd= 7 set0 = 8
            /// </summary>
            public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xEE, 3, 0, 0, 0, 0xCC };
            public int raiseTime = 5;
            public int lowerTime = 6;
            public int enableHyd = 7;
            public int set0 = 8;
        }

        //pgn instances

        /// <summary>
        /// autoSteerData - FE - 254 - 
        /// </summary>
        public CPGN_FE asData = new CPGN_FE();

        /// <summary>
        /// autoSteerReturn - FD - 253 - 
        /// </summary>
        public CPGN_FD asModule = new CPGN_FD();

        /// <summary>
        /// autoSteerSettings PGN - 252 - FC
        /// </summary>
        public CPGN_FC asSet = new CPGN_FC();

        /// <summary>
        /// autoSteerConfig PGN - 251 - FB
        /// </summary>
        public CPGN_FB asConfig = new CPGN_FB();

        /// <summary>
        /// machineData PGN - 239 - EF
        /// </summary>
        public CPGN_EF maData = new CPGN_EF();

        /// <summary>
        /// machineConfig PGN - 238 - EE
        /// </summary>
        public CPGN_EE maConfig = new CPGN_EE();
    }
}
