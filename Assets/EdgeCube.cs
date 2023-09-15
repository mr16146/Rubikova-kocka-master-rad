using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using static BeginnerAlgorithm;
using System.Linq;
using System.Text;

public class EdgeCube
{
    private Dictionary<CubeSide, CubeColor> colorBySide;
    private EdgeCubePosition position;

    public EdgeCube(EdgeCubePosition position)
    {
        this.position = position;
        this.colorBySide = new Dictionary<CubeSide, CubeColor>();
    }

    #region Properties

    public EdgeCubePosition Position
    {
        get { return this.position; }
    }

    public Dictionary<CubeSide, CubeColor> ColorBySide
    {
        get { return this.colorBySide; }
        set { this.colorBySide = value; }
    }

    #endregion

    public CubeSide GetSideByColor(CubeColor cubeColor)
    {
        foreach (KeyValuePair<CubeSide, CubeColor> sideAndColor in this.colorBySide)
            if (sideAndColor.Value == cubeColor)
                return sideAndColor.Key;

        return CubeSide.NoSide;
    }

    public CubeColor GetNonFrontColor()
    {
        return this.colorBySide.FirstOrDefault(sideAndColor => sideAndColor.Key != CubeSide.Front).Value;
    }
}