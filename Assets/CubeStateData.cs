using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using System.Linq;
using System.Text;

// Klasa sluzi za cuvanje svih podataka vezanih za stanje kocke
public class CubeStateData
{
    #region Dictionaries

    private Dictionary<CubeSide, CubeColor> colorBySide = new Dictionary<CubeSide, CubeColor>
    {
        { CubeSide.Front, CubeColor.Blue },
        { CubeSide.Back, CubeColor.Green },
        { CubeSide.Up, CubeColor.Yellow },
        { CubeSide.Down, CubeColor.White },
        { CubeSide.Left, CubeColor.Orange },
        { CubeSide.Right, CubeColor.Red }
    };

    private Dictionary<CubeColor, CubeSide> sideByColor = new Dictionary<CubeColor, CubeSide>
    {
        { CubeColor.Blue, CubeSide.Front },
        { CubeColor.Green, CubeSide.Back },
        { CubeColor.Yellow, CubeSide.Up },
        { CubeColor.White, CubeSide.Down },
        { CubeColor.Orange, CubeSide.Left },
        { CubeColor.Red, CubeSide.Right }
    };
    // Stanje kocke - za svaku stranicu se pamti niz od 9 boja, te boje predstavljaju boje polja stranica
    private Dictionary<CubeSide, CubeColor[]> cubeState = new Dictionary<CubeSide, CubeColor[]>
    {
        {CubeSide.Front, new CubeColor[9]},
        {CubeSide.Right, new CubeColor[9]},
        {CubeSide.Back, new CubeColor[9]},
        {CubeSide.Left, new CubeColor[9]},
        {CubeSide.Up, new CubeColor[9]},
        {CubeSide.Down, new CubeColor[9]},
    };
    // Za unetu stranicu vraca stranicu koja je iz trenutne perspektive na njenom mestu
    private Dictionary<CubeSide, KeyValuePair<CubeSide, bool>> sideToRotationMapping = new Dictionary<CubeSide, KeyValuePair<CubeSide, bool>>
    {
        { CubeSide.Front, new KeyValuePair<CubeSide, bool>(CubeSide.Front, true) },
        { CubeSide.Right, new KeyValuePair<CubeSide, bool>(CubeSide.Right, true) },
        { CubeSide.Back, new KeyValuePair<CubeSide, bool>(CubeSide.Back, true) },
        { CubeSide.Left, new KeyValuePair<CubeSide, bool>(CubeSide.Left, true) },
        { CubeSide.Up, new KeyValuePair<CubeSide, bool>(CubeSide.Up, true) },
        { CubeSide.Down, new KeyValuePair<CubeSide, bool>(CubeSide.Down, true) },
    };
    // Slika rotaciju/potez nerotirane kocke(pocetne perspektive) u rotaciju/potez u trenutnoj perspektivi kocke
    private Dictionary<CubeSide, CubeSide> rotationToSideMapping = new Dictionary<CubeSide, CubeSide>
    {
        { CubeSide.Front, CubeSide.Front },
        { CubeSide.Right, CubeSide.Right },
        { CubeSide.Back, CubeSide.Back },
        { CubeSide.Left, CubeSide.Left },
        { CubeSide.Up, CubeSide.Up },
        { CubeSide.Down, CubeSide.Down },
    };
    // Sluzi kao prelazno stanje kocke prilikom azuriranja stanja
    private Dictionary<CubeSide, CubeColor[]> newCubeState = new Dictionary<CubeSide, CubeColor[]>();
    private Dictionary<CubeSide, KeyValuePair<CubeSide, bool>> newSideToRotationMapping = new Dictionary<CubeSide, KeyValuePair<CubeSide, bool>>();
    private Dictionary<CubeSide, CubeSide> newRotationToSideMapping = new Dictionary<CubeSide, CubeSide>();

    #endregion

    public CubeStateData()
    {
        this.InitializeCubeState();
    }

    public CubeStateData(CubeStateData cubeStateData)
    {
        this.cubeState = cubeStateData.CubeState.ToDictionary(s => s.Key, s => Helper.DeepCopyColors(s.Value));
        this.newCubeState = cubeStateData.NewCubeState.ToDictionary(s => s.Key, s => Helper.DeepCopyColors(s.Value));
        this.sideToRotationMapping = cubeStateData.SideToRotationMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
        this.newSideToRotationMapping = cubeStateData.NewSideToRotationMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
        this.rotationToSideMapping = cubeStateData.RotationToSideMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
        this.newRotationToSideMapping = cubeStateData.NewRotationToSideMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
    }

    #region Properties

    public Dictionary<CubeSide, CubeColor[]> CubeState
    {
        get { return this.cubeState; }
        set { this.cubeState = value; }
    }

    public Dictionary<CubeSide, CubeColor[]> NewCubeState
    {
        get { return this.newCubeState; }
        set { this.newCubeState = value; }
    }

    public Dictionary<CubeSide, KeyValuePair<CubeSide, bool>> SideToRotationMapping
    {
        get { return this.sideToRotationMapping; }
        set { this.sideToRotationMapping = value; }
    }

    public Dictionary<CubeSide, KeyValuePair<CubeSide, bool>> NewSideToRotationMapping
    {
        get { return this.newSideToRotationMapping; }
        set { this.newSideToRotationMapping = value;}
    }

    public Dictionary<CubeSide, CubeSide> RotationToSideMapping
    {
        get { return this.rotationToSideMapping; }
        set { this.rotationToSideMapping = value; }
    }

    public Dictionary<CubeSide, CubeSide> NewRotationToSideMapping
    {
        get { return this.newRotationToSideMapping; }
        set { this.newRotationToSideMapping = value; }
    }

    public Dictionary<CubeColor, CubeSide> SideByColor
    {
        get { return this.sideByColor; }
    }

    #endregion

    private void InitializeCubeState()
    {
        foreach (KeyValuePair<CubeSide, CubeColor[]> cubeSideState in cubeState)
        {
            for (int i = 0; i < 9; i++)
            {
                cubeSideState.Value[i] = this.colorBySide[cubeSideState.Key];
            }
        }
    }
}