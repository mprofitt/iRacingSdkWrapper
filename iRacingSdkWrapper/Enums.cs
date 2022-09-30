using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace iRacingSdkWrapper
{
    public enum TrackSurfaces
    {
        NotInWorld = -1,
        OffTrack,
        InPitStall,
        AproachingPits,
        OnTrack
    }

    public enum SessionStates
    {
        Invalid,
        GetInCar,
        Warmup,
        ParadeLaps,
        Racing,
        Checkered,
        CoolDown
    }

    public enum CarLeftRight
    {
        LROff,
        LRClear,          // no cars around us.
        LRCarLeft,        // there is a car to our left.
        LRCarRight,       // there is a car to our right.
        LRCarLeftRight,   // there are cars on each side.
        LR2CarsLeft,      // there are two cars to our left.
        LR2CarsRight      // there are two cars to our right.
    }
    public enum PaceMode
    {
        [Description("single file start")]
        SingleFileStart,
        [Description("double file start")]
        DoubleFileStart,
        [Description("single file restart")]
        SingleFileRestart,
        [Description("double file restart")]
        DoubleFileRestart,
        [Description("not Pacing")]
        NotPacing,
    }

    public enum PaceFlags
    {
        EndOfLine = 0x01,
        FreePass = 0x02,
        WavedAround = 0x04,
    }

    public enum PaceLine
    {
        None = -1,
        Inside,
        Outside,
    }

    public enum TrackSurfaceMaterial
    {
        SurfaceNotInWorld = -1,
        UndefinedMaterial = 0,

        Asphalt1Material,
        Asphalt2Material,
        Asphalt3Material,
        Asphalt4Material,
        Concrete1Material,
        Concrete2Material,
        RacingDirt1Material,
        RacingDirt2Material,
        Paint1Material,
        Paint2Material,
        Rumble1Material,
        Rumble2Material,
        Rumble3Material,
        Rumble4Material,

        Grass1Material,
        Grass2Material,
        Grass3Material,
        Grass4Material,
        Dirt1Material,
        Dirt2Material,
        Dirt3Material,
        Dirt4Material,
        SandMaterial,
        Gravel1Material,
        Gravel2Material,
        GrasscreteMaterial,
        AstroturfMaterial,
    }
    [Flags]
    public enum PitServiceFlags
    {
        LFTireChange = 0x01,
        RFTireChange = 0x02,
        LRTireCHange = 0x04,
        RRTireChange = 0x08,
        FuelFill = 0x10,
        WinshieldTearoff = 0x20,
        FastRepair = 0x40,
    }
    public enum PitSvStatus
    {
        // status
        PitSvNone = 0,
        PitSvInProgress,
        PitSvComplete,

        // errors
        PitSvTooFarLeft = 100,
        PitSvTooFarRight,
        PitSvTooFarForward,
        PitSvTooFarBack,
        PitSvBadAngle,
        PitSvCantFixThat,
    };

    public enum PitAction
    {
        None,
        PitEntry,
        PitExit
    }
    [Flags]
    public enum SessionFlags : uint
    {
        Checkered = 0x00000001,
        White = 0x00000002,
        Green = 0x00000004,
        Yellow = 0x00000008,
        Red = 0x00000010,
        Blue = 0x00000020,
        Debris = 0x00000040,
        Crossed = 0x00000080,
        YellowWaving = 0x00000100,
        OneLapToGreen = 0x00000200,
        GreenHeld = 0x00000400,
        TenToGo = 0x00000800,
        FiveToGo = 0x00001000,
        RandomWaving = 0x00002000,
        Caution = 0x00004000,
        CautionWaving = 0x00008000,

        Black = 0x00010000,
        Disqualify = 0x00020000,
        Servicible = 0x00040000, // car is allowed service (not a flag)
        Furled = 0x00080000,
        Repair = 0x00100000,

        StartHidden = 0x10000000,
        StartReady = 0x20000000,
        StartSet = 0x40000000,
        StartGo = 0x80000000,
    }
}
