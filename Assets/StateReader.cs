using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StateReader : MonoBehaviour
{
    public GameObject Cube;
    // Ovaj omotac cuva stanje 3D modela kocke
    private CubeStateWrapper cubeStateWrapper;
    // Ovaj omotac sluzi iskljucivo za privremena izracunavanja u algoritmima.
    // Na pocetku algoritma se iskopira stanje 3D modela u stanje ovog omotaca i nad njim se izvrsava algoritam.
    private CubeStateWrapper solvingCubeStateWrapper;

    void Start()
    {
        this.cubeStateWrapper = new CubeStateWrapper(new CubeStateData());
        this.solvingCubeStateWrapper = new CubeStateWrapper(new CubeStateData());
    }

    private void Update()
    {

    }

    #region Properties

    public CubeStateWrapper CubeStateWrapper
    {
        get { return cubeStateWrapper; }
    }

    public CubeStateWrapper SolvingCubeStateWrapper
    {
        get { return solvingCubeStateWrapper; }
    }

    #endregion

    #region Enums

    public enum CubeColor
    {
        NoColor,
        Red,
        Green,
        Blue,
        Orange,
        White,
        Yellow
    }

    public enum CubeSide
    {
        NoSide,
        Front,
        Right,
        Back,
        Left,
        Up,
        Down
    }

    #endregion
}