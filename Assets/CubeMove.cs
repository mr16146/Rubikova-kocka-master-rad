using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using System.Linq;
using System.Text;

public class CubeMove
{
    private CubeSide cubeSide;
    private bool clockwise;
    private bool doubleMove;

    public CubeMove(CubeSide cubeSide, bool clockwise = true, bool doubleMove = false)
    {
        this.cubeSide = cubeSide;
        this.clockwise = clockwise;
        this.doubleMove = doubleMove;
    }

    public CubeSide CubeSide
    {
        get { return cubeSide; }
        set { this.cubeSide = value; }
    }

    public bool Clockwise { get { return clockwise; } }
    public bool DoubleMove { get { return doubleMove; } }
}