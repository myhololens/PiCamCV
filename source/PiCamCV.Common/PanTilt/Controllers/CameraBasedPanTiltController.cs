﻿using System;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV.Structure;
using PiCamCV.Common;
using PiCamCV.Common.ExtensionMethods;
using PiCamCV.Common.Interfaces;
using PiCamCV.ConsoleApp.Runners.PanTilt.MoveStrategies;
using PiCamCV.Interfaces;

namespace PiCamCV.ConsoleApp.Runners.PanTilt
{
    public class CameraPanTiltProcessOutput : CameraProcessOutput
    {
        public PanTiltSetting PanTiltPrior { get; set; }

        public PanTiltSetting PanTiltNow { get; set; }

        public Point Target { get; set; }

        public override string ToString()
        {
            return string.Format("PositionPrior={0}, Target={1}, PositionNow={2}", PanTiltPrior, Target, PanTiltNow);
        }
    }

    public interface IController
    {
        
    }

    public abstract class CameraBasedPanTiltController : 
        PanTiltController
        , IController
        , ICameraProcessor<CameraProcessInput, CameraPanTiltProcessOutput>
    {
        protected CaptureConfig CaptureConfig { get; private set; }

        protected Point CentrePoint { get; private set; }

        protected int Ticks { get;set;}

        protected CameraBasedPanTiltController(IPanTiltMechanism panTiltMech, CaptureConfig captureConfig, IScreen screen)
            : base(panTiltMech, screen)
        {
            CaptureConfig = captureConfig;
            CentrePoint = CaptureConfig.GetCenter();

            Log.InfoFormat("Centre = {0}", CentrePoint);
            Ticks = 0;

            MoveTo(new PanTiltSetting(50, 50));
        }


        protected CameraPanTiltProcessOutput ReactToTarget(Point targetPoint)
        {
            var output = new CameraPanTiltProcessOutput();
            var moveStrategy = new CameraModifierStrategy(CaptureConfig, Screen, targetPoint, CentrePoint);
            var newPosition = moveStrategy.CalculateNewSetting(CurrentSetting);

            output.Target = targetPoint;
            output.PanTiltPrior = CurrentSetting.Clone();
            output.PanTiltNow = newPosition.Clone();

            if (!targetPoint.Equals(CentrePoint))
            {
                MoveTo(newPosition);
            }

            Screen.WriteLine("Capture Config {0}", CaptureConfig);
            Screen.WriteLine("Target {0}", targetPoint);
            ScreenWritePanTiltSettings();

            return output;
        }

        public CameraPanTiltProcessOutput Process(CameraProcessInput input)
        {
            var stopWatch = Stopwatch.StartNew();
            var result = DoProcess(input);

            if (result.CapturedImage == null && input.SetCapturedImage)
            {
                result.CapturedImage = input.Captured.ToImage<Bgr, byte>();
            }

            result.Elapsed = stopWatch.Elapsed;
            return result;
        }
        protected abstract CameraPanTiltProcessOutput DoProcess(CameraProcessInput input);
    }
}