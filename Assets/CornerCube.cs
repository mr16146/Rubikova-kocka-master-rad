using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using static BeginnerAlgorithm;
using System.Linq;
using System.Text;

public class CornerCube
{
    private Dictionary<CubeSide, CubeColor> colorBySide;
    private CornerCubePosition position;

    public CornerCube(CornerCubePosition position)
    {
        this.position = position;
        this.colorBySide = new Dictionary<CubeSide, CubeColor>();
    }

    #region Properties

    public CornerCubePosition Position
    {
        get { return this.position; }
    }

    public Dictionary<CubeSide, CubeColor> ColorBySide
    {
        get { return colorBySide; }
        set { colorBySide = value; }
    }

    #endregion

    public CubeSide GetSideByColor(CubeColor cubeColor)
    {
        foreach (KeyValuePair<CubeSide, CubeColor> sideAndColor in this.colorBySide)
            if (sideAndColor.Value == cubeColor)
                return sideAndColor.Key;

        return CubeSide.NoSide;
    }
}