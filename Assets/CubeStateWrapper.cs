using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kociemba;
using CubeColor = StateReader.CubeColor;
using CubeSide = StateReader.CubeSide;
using System.Linq;
using System.Text;
using System;

// Omotac stanja kocke koji omogucava manipulisanje istog tog stanja, tj. azurira podatke kocke prilikom rotacije kocke.
// Metode iz ove klase sluze za azuriranje stanja kocke nakon odredjene rotacije kocke ili stranice.
public class CubeStateWrapper
{
    private CubeStateData cubeStateData;

    public CubeStateWrapper(CubeStateData cubeStateData)
    {
        this.cubeStateData = cubeStateData;
    }

    #region Properties

    public CubeStateData CubeStateData
    {
        get { return this.cubeStateData; }
        set { this.cubeStateData = value; }
    }

    #endregion


    #region CubeRotations

    public void RotateCube(CubeRotation.CubeRotationDirection cubeRotationDirection)
    {
        this.cubeStateData.NewSideToRotationMapping = this.cubeStateData.SideToRotationMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);
        this.cubeStateData.NewRotationToSideMapping = this.cubeStateData.RotationToSideMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);

        switch (cubeRotationDirection)
        {
            case CubeRotation.CubeRotationDirection.UpRight:
                this.CubeRotateUpRight();
                break;
            case CubeRotation.CubeRotationDirection.DownRight:
                this.CubeRotateDownRight();
                break;
            case CubeRotation.CubeRotationDirection.UpLeft:
                this.CubeRotateUpLeft();
                break;
            case CubeRotation.CubeRotationDirection.DownLeft:
                this.CubeRotateDownLeft();
                break;
            case CubeRotation.CubeRotationDirection.Right:
                this.CubeRotateRight();
                break;
            case CubeRotation.CubeRotationDirection.Left:
                this.CubeRotateLeft();
                break;
        }

        this.cubeStateData.SideToRotationMapping = this.cubeStateData.NewSideToRotationMapping;
        this.cubeStateData.RotationToSideMapping = this.cubeStateData.NewRotationToSideMapping;
    }

    private void CubeRotateRight()
    {
        this.UpdateSideToRotation(CubeSide.Right, CubeSide.Front);
        this.UpdateSideToRotation(CubeSide.Left, CubeSide.Back);
        this.UpdateSideToRotation(CubeSide.Front, CubeSide.Left);
        this.UpdateSideToRotation(CubeSide.Back, CubeSide.Right);

        this.UpdateRotationToSide(CubeSide.Front, CubeSide.Right);
        this.UpdateRotationToSide(CubeSide.Back, CubeSide.Left);
        this.UpdateRotationToSide(CubeSide.Left, CubeSide.Front);
        this.UpdateRotationToSide(CubeSide.Right, CubeSide.Back);
    }

    private void CubeRotateUpRight()
    {
        this.UpdateSideToRotation(CubeSide.Right, CubeSide.Down);
        this.UpdateSideToRotation(CubeSide.Up, CubeSide.Right);
        this.UpdateSideToRotation(CubeSide.Left, CubeSide.Up);
        this.UpdateSideToRotation(CubeSide.Down, CubeSide.Left);

        this.UpdateRotationToSide(CubeSide.Right, CubeSide.Up);
        this.UpdateRotationToSide(CubeSide.Left, CubeSide.Down);
        this.UpdateRotationToSide(CubeSide.Up, CubeSide.Left);
        this.UpdateRotationToSide(CubeSide.Down, CubeSide.Right);
    }

    private void CubeRotateLeft()
    {
        this.UpdateSideToRotation(CubeSide.Right, CubeSide.Back);
        this.UpdateSideToRotation(CubeSide.Left, CubeSide.Front);
        this.UpdateSideToRotation(CubeSide.Front, CubeSide.Right);
        this.UpdateSideToRotation(CubeSide.Back, CubeSide.Left);

        this.UpdateRotationToSide(CubeSide.Back, CubeSide.Right);
        this.UpdateRotationToSide(CubeSide.Front, CubeSide.Left);
        this.UpdateRotationToSide(CubeSide.Right, CubeSide.Front);
        this.UpdateRotationToSide(CubeSide.Left, CubeSide.Back);
    }

    private void CubeRotateUpLeft()
    {
        this.UpdateSideToRotation(CubeSide.Front, CubeSide.Down);
        this.UpdateSideToRotation(CubeSide.Back, CubeSide.Up);
        this.UpdateSideToRotation(CubeSide.Up, CubeSide.Front);
        this.UpdateSideToRotation(CubeSide.Down, CubeSide.Back);

        this.UpdateRotationToSide(CubeSide.Back, CubeSide.Down);
        this.UpdateRotationToSide(CubeSide.Front, CubeSide.Up);
        this.UpdateRotationToSide(CubeSide.Up, CubeSide.Back);
        this.UpdateRotationToSide(CubeSide.Down, CubeSide.Front);
    }

    private void CubeRotateDownLeft()
    {
        this.UpdateSideToRotation(CubeSide.Front, CubeSide.Up);
        this.UpdateSideToRotation(CubeSide.Back, CubeSide.Down);
        this.UpdateSideToRotation(CubeSide.Up, CubeSide.Back);
        this.UpdateSideToRotation(CubeSide.Down, CubeSide.Front);

        this.UpdateRotationToSide(CubeSide.Back, CubeSide.Up);
        this.UpdateRotationToSide(CubeSide.Front, CubeSide.Down);
        this.UpdateRotationToSide(CubeSide.Up, CubeSide.Front);
        this.UpdateRotationToSide(CubeSide.Down, CubeSide.Back);
    }

    private void CubeRotateDownRight()
    {
        this.UpdateSideToRotation(CubeSide.Right, CubeSide.Up);
        this.UpdateSideToRotation(CubeSide.Up, CubeSide.Left);
        this.UpdateSideToRotation(CubeSide.Left, CubeSide.Down);
        this.UpdateSideToRotation(CubeSide.Down, CubeSide.Right);

        this.UpdateRotationToSide(CubeSide.Right, CubeSide.Down);
        this.UpdateRotationToSide(CubeSide.Left, CubeSide.Up);
        this.UpdateRotationToSide(CubeSide.Up, CubeSide.Right);
        this.UpdateRotationToSide(CubeSide.Down, CubeSide.Left);
    }

    private void UpdateSideToRotation(CubeSide newSide, CubeSide currentSide, bool switchDirection = false)
    {
        if (!switchDirection)
            this.cubeStateData.NewSideToRotationMapping[newSide] = this.cubeStateData.SideToRotationMapping[currentSide];
        else
        {
            var switchedSideRotation = new KeyValuePair<CubeSide, bool>(currentSide, !this.cubeStateData.SideToRotationMapping[currentSide].Value);
            this.cubeStateData.NewSideToRotationMapping[newSide] = switchedSideRotation;
        }
    }

    private void UpdateRotationToSide(CubeSide newSide, CubeSide currentSide)
    {
        this.cubeStateData.NewRotationToSideMapping[this.cubeStateData.SideToRotationMapping[newSide].Key] = this.cubeStateData.RotationToSideMapping[this.cubeStateData.SideToRotationMapping[currentSide].Key];
    }

    #endregion

    #region FaceRotations

    public void RotateFace(CubeMove cubeMove)
    {
        var rotateSide = this.cubeStateData.SideToRotationMapping[cubeMove.CubeSide];
        int rotateCount = cubeMove.Clockwise ? 1 : 3;

        for (int i = 0; i < rotateCount; i++)
        {
            this.cubeStateData.NewCubeState = this.cubeStateData.CubeState.ToDictionary(s => s.Key, s => Helper.DeepCopyColors(s.Value));

            switch (rotateSide.Key)
            {
                case CubeSide.Right:
                    this.FaceRotateRightClockwise();
                    break;
                case CubeSide.Left:
                    this.FaceRotateLeftClockwise();
                    break;
                case CubeSide.Front:
                    this.FaceRotateFrontClockwise();
                    break;
                case CubeSide.Back:
                    this.FaceRotateBackClockwise();
                    break;
                case CubeSide.Up:
                    this.FaceRotateUpClockwise();
                    break;
                case CubeSide.Down:
                    this.FaceRotateDownClockwise();
                    break;
            }

            this.cubeStateData.CubeState = this.cubeStateData.NewCubeState;
        }

        if (cubeMove.DoubleMove)
            this.RotateFace(new CubeMove(cubeMove.CubeSide, cubeMove.Clockwise));
    }

    private void FaceRotateUpClockwise()
    {
        this.SwapSideColors(CubeSide.Front, 0, CubeSide.Right, 0);
        this.SwapSideColors(CubeSide.Front, 1, CubeSide.Right, 1);
        this.SwapSideColors(CubeSide.Front, 2, CubeSide.Right, 2);

        this.SwapSideColors(CubeSide.Right, 0, CubeSide.Back, 0);
        this.SwapSideColors(CubeSide.Right, 1, CubeSide.Back, 1);
        this.SwapSideColors(CubeSide.Right, 2, CubeSide.Back, 2);

        this.SwapSideColors(CubeSide.Back, 0, CubeSide.Left, 0);
        this.SwapSideColors(CubeSide.Back, 1, CubeSide.Left, 1);
        this.SwapSideColors(CubeSide.Back, 2, CubeSide.Left, 2);

        this.SwapSideColors(CubeSide.Left, 0, CubeSide.Front, 0);
        this.SwapSideColors(CubeSide.Left, 1, CubeSide.Front, 1);
        this.SwapSideColors(CubeSide.Left, 2, CubeSide.Front, 2);

        this.RotateOneSideColorsClockwise(CubeSide.Up);
    }

    private void FaceRotateDownClockwise()
    {
        this.SwapSideColors(CubeSide.Front, 6, CubeSide.Left, 6);
        this.SwapSideColors(CubeSide.Front, 7, CubeSide.Left, 7);
        this.SwapSideColors(CubeSide.Front, 8, CubeSide.Left, 8);

        this.SwapSideColors(CubeSide.Left, 6, CubeSide.Back, 6);
        this.SwapSideColors(CubeSide.Left, 7, CubeSide.Back, 7);
        this.SwapSideColors(CubeSide.Left, 8, CubeSide.Back, 8);

        this.SwapSideColors(CubeSide.Back, 6, CubeSide.Right, 6);
        this.SwapSideColors(CubeSide.Back, 7, CubeSide.Right, 7);
        this.SwapSideColors(CubeSide.Back, 8, CubeSide.Right, 8);

        this.SwapSideColors(CubeSide.Right, 6, CubeSide.Front, 6);
        this.SwapSideColors(CubeSide.Right, 7, CubeSide.Front, 7);
        this.SwapSideColors(CubeSide.Right, 8, CubeSide.Front, 8);

        this.RotateOneSideColorsClockwise(CubeSide.Down);
    }

    private void FaceRotateFrontClockwise()
    {
        this.SwapSideColors(CubeSide.Up, 6, CubeSide.Left, 8);
        this.SwapSideColors(CubeSide.Up, 7, CubeSide.Left, 5);
        this.SwapSideColors(CubeSide.Up, 8, CubeSide.Left, 2);

        this.SwapSideColors(CubeSide.Left, 2, CubeSide.Down, 0);
        this.SwapSideColors(CubeSide.Left, 5, CubeSide.Down, 1);
        this.SwapSideColors(CubeSide.Left, 8, CubeSide.Down, 2);

        this.SwapSideColors(CubeSide.Down, 0, CubeSide.Right, 6);
        this.SwapSideColors(CubeSide.Down, 1, CubeSide.Right, 3);
        this.SwapSideColors(CubeSide.Down, 2, CubeSide.Right, 0);

        this.SwapSideColors(CubeSide.Right, 0, CubeSide.Up, 6);
        this.SwapSideColors(CubeSide.Right, 3, CubeSide.Up, 7);
        this.SwapSideColors(CubeSide.Right, 6, CubeSide.Up, 8);

        this.RotateOneSideColorsClockwise(CubeSide.Front);
    }

    private void FaceRotateBackClockwise()
    {
        this.SwapSideColors(CubeSide.Up, 0, CubeSide.Right, 2);
        this.SwapSideColors(CubeSide.Up, 1, CubeSide.Right, 5);
        this.SwapSideColors(CubeSide.Up, 2, CubeSide.Right, 8);

        this.SwapSideColors(CubeSide.Right, 2, CubeSide.Down, 8);
        this.SwapSideColors(CubeSide.Right, 5, CubeSide.Down, 7);
        this.SwapSideColors(CubeSide.Right, 8, CubeSide.Down, 6);

        this.SwapSideColors(CubeSide.Down, 6, CubeSide.Left, 0);
        this.SwapSideColors(CubeSide.Down, 7, CubeSide.Left, 3);
        this.SwapSideColors(CubeSide.Down, 8, CubeSide.Left, 6);

        this.SwapSideColors(CubeSide.Left, 0, CubeSide.Up, 2);
        this.SwapSideColors(CubeSide.Left, 3, CubeSide.Up, 1);
        this.SwapSideColors(CubeSide.Left, 6, CubeSide.Up, 0);

        this.RotateOneSideColorsClockwise(CubeSide.Back);

    }

    private void FaceRotateRightClockwise()
    {
        this.SwapSideColors(CubeSide.Front, 2, CubeSide.Down, 2);
        this.SwapSideColors(CubeSide.Front, 5, CubeSide.Down, 5);
        this.SwapSideColors(CubeSide.Front, 8, CubeSide.Down, 8);

        this.SwapSideColors(CubeSide.Up, 2, CubeSide.Front, 2);
        this.SwapSideColors(CubeSide.Up, 5, CubeSide.Front, 5);
        this.SwapSideColors(CubeSide.Up, 8, CubeSide.Front, 8);

        this.SwapSideColors(CubeSide.Back, 0, CubeSide.Up, 8);
        this.SwapSideColors(CubeSide.Back, 3, CubeSide.Up, 5);
        this.SwapSideColors(CubeSide.Back, 6, CubeSide.Up, 2);

        this.SwapSideColors(CubeSide.Down, 2, CubeSide.Back, 6);
        this.SwapSideColors(CubeSide.Down, 5, CubeSide.Back, 3);
        this.SwapSideColors(CubeSide.Down, 8, CubeSide.Back, 0);

        this.RotateOneSideColorsClockwise(CubeSide.Right);
    }

    private void FaceRotateLeftClockwise()
    {
        this.SwapSideColors(CubeSide.Front, 0, CubeSide.Up, 0);
        this.SwapSideColors(CubeSide.Front, 3, CubeSide.Up, 3);
        this.SwapSideColors(CubeSide.Front, 6, CubeSide.Up, 6);

        this.SwapSideColors(CubeSide.Up, 0, CubeSide.Back, 8);
        this.SwapSideColors(CubeSide.Up, 3, CubeSide.Back, 5);
        this.SwapSideColors(CubeSide.Up, 6, CubeSide.Back, 2);

        this.SwapSideColors(CubeSide.Back, 2, CubeSide.Down, 6);
        this.SwapSideColors(CubeSide.Back, 5, CubeSide.Down, 3);
        this.SwapSideColors(CubeSide.Back, 8, CubeSide.Down, 0);

        this.SwapSideColors(CubeSide.Down, 0, CubeSide.Front, 0);
        this.SwapSideColors(CubeSide.Down, 3, CubeSide.Front, 3);
        this.SwapSideColors(CubeSide.Down, 6, CubeSide.Front, 6);

        this.RotateOneSideColorsClockwise(CubeSide.Left);
    }

    private void SwapSideColors(CubeSide newSide, int newCubiePosition, CubeSide currentSide, int currentCubiePosition)
    {
        this.cubeStateData.NewCubeState[newSide][newCubiePosition] = this.cubeStateData.CubeState[currentSide][currentCubiePosition];
    }

    private void RotateOneSideColorsClockwise(CubeSide cubeSide)
    {
        this.SwapSideColors(cubeSide, 0, cubeSide, 6);
        this.SwapSideColors(cubeSide, 1, cubeSide, 3);
        this.SwapSideColors(cubeSide, 2, cubeSide, 0);
        this.SwapSideColors(cubeSide, 3, cubeSide, 7);
        this.SwapSideColors(cubeSide, 5, cubeSide, 1);
        this.SwapSideColors(cubeSide, 6, cubeSide, 8);
        this.SwapSideColors(cubeSide, 7, cubeSide, 5);
        this.SwapSideColors(cubeSide, 8, cubeSide, 2);
    }

    #endregion
}