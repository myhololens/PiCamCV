﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Common.Logging;
using Emgu.CV;
using Kraken.Core;
using PiCamCV.Common;
using PiCamCV.ConsoleApp.Runners;
using PiCamCV.ConsoleApp.Runners.PanTilt;
using PiCamCV.Interfaces;
using Raspberry.IO.Components.Controllers.Pca9685;
using RPi.Pwm;

namespace PiCamCV.ConsoleApp
{
    /// <summary>
    /// mono picamcv.con.exe -m=simple
    /// </summary>
    class Program
    {

        protected static ILog Log = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            var appData = ExecutionEnvironment.GetApplicationMetadata();
            Log.Info(appData);

            var options = new ConsoleOptions(args);

            if (options.ShowHelp)
            {
                Console.WriteLine("Options:");
                options.OptionSet.WriteOptionDescriptions(Console.Out);
                return;
            }

            ICaptureGrab capture = null;


            CapturePi.DoMatMagic("CreateCapture");

            var noCaptureGrabs = new[] { Mode.simple, Mode.pantiltjoy };
            var i2cRequired = new[] { Mode.pantiltface, Mode.pantiltjoy };
            CaptureConfig captureConfig = null;
            if (!noCaptureGrabs.Contains(options.Mode))
            {
                var request = new CaptureRequest { Device = CaptureDevice.Usb };
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    request.Device = CaptureDevice.Pi;
                }

                request.Config = new CaptureConfig { Width = 128, Height = 96, Framerate = 10, Monochrome = true };

                capture = CaptureFactory.GetCapture(request);
                captureConfig = capture.GetCaptureProperties();
                Log.Info(m => m("Capture properties read: {0}", captureConfig));

                SafetyCheckRoi(options, captureConfig);
            }

            IPanTiltMechanism panTiltMech = null;
            IScreen screen = null;
            if (i2cRequired.Contains(options.Mode))
            {
                var pwmDeviceFactory = new Pca9685DeviceFactory();
                var pwmDevice = pwmDeviceFactory.GetDevice(options.UseFakeDevice);
                panTiltMech = new PanTiltMechanism(pwmDevice);
                screen = new ConsoleScreen();
            }

            IRunner runner;
            Log.Info(options);
            switch (options.Mode)
            {
                case Mode.noop: runner = new NoopRunner(capture);
                    break;

                case Mode.simple:runner = new SimpleCv(); 
                    break;

                case Mode.colourdetect:
                    var colorDetector = new ColorDetectRunner(capture);
                    if (options.HasColourSettings)
                    {
                        colorDetector.Settings = options.ColourSettings;
                    }
                    runner = colorDetector;
                    break;

                case Mode.haar:
                    var relativePath = string.Format(@"haarcascades{0}haarcascade_frontalface_default.xml", Path.DirectorySeparatorChar);
                    var cascadeFilename = Path.Combine(appData.ExeFolder, relativePath);
                    var cascadeContent = File.ReadAllText(cascadeFilename);
                    runner = new CascadeRunner(capture, cascadeContent);
                    break;

                case Mode.servosort:
                    runner = new ServoSorter(capture, options);
                    break;

                case Mode.pantiltjoy:
                    var joyController = new JoystickPanTiltController(panTiltMech, screen);
                    runner = new TimerRunner(joyController);
                    break;

                case Mode.pantiltface:
                    var controllerF = new FaceTrackingPanTiltController(panTiltMech, captureConfig, screen);
                    runner = new CameraBasedPanTiltRunner(panTiltMech, capture, controllerF, screen);
                    break;

                case Mode.pantiltcolour:
                    var controllerC = new ColourTrackingPanTiltController(panTiltMech, captureConfig, screen);
                    if (options.HasColourSettings)
                    {
                        controllerC.Settings = options.ColourSettings;
                    }
                    runner = new CameraBasedPanTiltRunner(panTiltMech, capture, controllerC, screen);
                    break;

                default:
                    throw KrakenException.Create("Option mode {0} needs wiring up", options.Mode);
            }

            runner.Run();
        }

        private static void SafetyCheckRoi(ConsoleOptions options, CaptureConfig captureProperties)
        {
            if (
                captureProperties.Width != 0   && 
                captureProperties.Height != 0  &&
                options.ColourSettings != null
                )
            {
                var roiWidthTooBig = options.ColourSettings.Roi.Width >     captureProperties.Width;
                var roiHeightTooBig = options.ColourSettings.Roi.Height >   captureProperties.Height;
                if (roiWidthTooBig || roiHeightTooBig)
                {
                    Log.Warn("ROI is too big! Ignoring");
                    options.ColourSettings.Roi = Rectangle.Empty;
                }
            }
        }
    }
}
