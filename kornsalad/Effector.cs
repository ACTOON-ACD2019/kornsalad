﻿using OpenCvSharp;
using OpenCvSharp.Util;
using OpenCvSharp.XImgProc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Text;



namespace kornsalad
{
    public class Effector
    {
        Mat Image { get; set; }
        int ImageHeight { get; set; }
        int ImageWidth { get; set; }
        int ImageChannel { get; set; }
        Size ImageSize { get; set; }
        Size SceneSize { get; set; }
        double Framerate { get; set; }

        VideoWriter TempWriter { get; set; }

        Mat BaseMat { get; set; }

        string Filename { get; set; }
        Point Position { get; set; }
        int CurrentLayer { get; set; }

        IEnumerator<Mat> PreLoadedMats;
        List<Mat> PreWrittenMats;

        /// <summary>
        /// Make a effect base with provided image
        /// </summary>
        /// <param name="inputImageName"></param>
        /// <param name="frameRate"></param>
        /// <param name="sceneSize"></param>
        public Effector(double frameRate, Size sceneSize)
        {
            Framerate = frameRate;
            SceneSize = sceneSize;
        }

        public void Initialize(string bgColor, string preFormat = "mp4v")
        {
            PreWrittenMats = new List<Mat>();

            if (bgColor == "white")
                BaseMat = new Mat(SceneSize, MatType.CV_8UC4, new Scalar(255, 255, 255, 255)); // white background
            else
                BaseMat = new Mat(SceneSize, MatType.CV_8UC4, new Scalar(0, 0, 0, 255)); // black background

            Filename = Renamer.CreateANewName(directory: ".", extension: "mp4");
            Console.WriteLine("[x] initializing a new video file {0}", Filename);

            CurrentLayer = 0;
        }

        public string Encode()
        {
            VideoWriter writer = new VideoWriter(Filename, "mp4v", Framerate, SceneSize);

            Console.WriteLine("[x] closing a new video file {0}", Filename);

            foreach (var frame in PreWrittenMats)
                writer.Write(frame);

            writer.Release();
            PreWrittenMats.Clear();

            return Filename;
        }

        public Effector AddLayer(string inputImageName, int posx = -1, int posy = -1)
        {
            ++CurrentLayer;

            if (PreWrittenMats.Count() > 0)
                PreLoadedMats = PreWrittenMats.GetEnumerator();
            
            Image = Cv2.ImRead(inputImageName, ImreadModes.Unchanged);
            ImageHeight = Image.Height;
            ImageWidth = Image.Width;
            ImageChannel = Image.Channels();
            ImageSize = new Size(ImageWidth, ImageHeight);

            if (posx != -1)
                Position = new Point(posx, posy);
            else
                Position = new Point(
                    SceneSize.Width / 2 - ImageSize.Width / 2,
                    SceneSize.Height / 2 - ImageSize.Height / 2
                );

            return this;
        }

        private Mat _Translate(Mat image, int x, int y)
        {
            var array = new float[,] {
                        { 1, 0, x},
                        { 0, 1, y}
                };

            Mat filter = new Mat(2, 3, MatType.CV_32F);
            filter.SetArray(0, 0, array);

            return image.WarpAffine(filter, ImageSize);
        }

        private Mat _Rotate(Mat image, int angle, float[] center=null, double scale=1.0)
        {
            Mat M = Cv2.GetRotationMatrix2D(new Point2f(image.Width / 2, image.Height / 2), angle, 1);
            Mat dst = new Mat();
            Cv2.WarpAffine(image, dst, M, image.Size());
            return dst;
        }
        
        private Mat AlphaBlending(Mat data, int posX, int posY)
        {
            var dataWidth = data.Size().Width;
            var dataHeight = data.Size().Height;
            var dataToBeFilled = new Scalar(0, 0, 0, 0);

            Mat newMat;

            newMat = data.CopyMakeBorder(
                    top: posY,
                    bottom: SceneSize.Height - dataHeight - posY,
                    left: posX,
                    right: SceneSize.Width - dataWidth - posX,
                    borderType: BorderTypes.Constant,
                    value: dataToBeFilled
                );

            if (PreLoadedMats != null)
            {
                PreLoadedMats.MoveNext();

                var baseimg = PreLoadedMats.Current;
                var newalpha = newMat.Split()[3];

                Mat background = new Mat(SceneSize, MatType.CV_8UC3, 0);
                Mat foreground = new Mat(SceneSize, MatType.CV_8UC3, 0);
                Mat alpha = new Mat(SceneSize, MatType.CV_8UC3, 0);

                baseimg = baseimg.CvtColor(ColorConversionCodes.RGBA2RGB);
                baseimg.ConvertTo(background, MatType.CV_8UC3);

                newMat = newMat.CvtColor(ColorConversionCodes.RGBA2RGB);
                newMat.ConvertTo(foreground, MatType.CV_8UC3);

                newalpha = newalpha.CvtColor(ColorConversionCodes.RGBA2RGB);
                newalpha.ConvertTo(alpha, MatType.CV_8UC3);

                Cv2.Multiply(alpha, foreground, foreground);
                Cv2.Multiply(1 - (alpha / 255.0), background, background);
                Cv2.Add(background, foreground, newMat);

                background.Dispose();
                foreground.Dispose();
                alpha.Dispose();
                BaseMat.Dispose();
                newalpha.Dispose();
            }
            else
                newMat.CopyTo(BaseMat, newMat.Split()[3]);

            return newMat;
        }

        public Effector None(int duration)
        {
            PreWrittenMats = new List<Mat>();

            for (var i = 0; i < Framerate * duration; i++)
            {
                var mat = AlphaBlending(Image, Position.X, Position.Y);
                PreWrittenMats.Add(mat);
            }

            Cv2.ImWrite(string.Format("__preframe_{0}.jpg", CurrentLayer), PreWrittenMats[0]);

            return this;
        }

        public Effector Earthquake(int earthPower, int earthSpeed, int earthTime)
        {
            PreWrittenMats = new List<Mat>();

            Random rd = new Random();
            
            for (var i = 0; i < Framerate * earthTime; i++)
            {
                if ((i % earthSpeed == 0) || (i == 0))
                {
                    var result = _Translate(Image,
                        rd.Next(-earthPower, earthPower),
                        rd.Next(-earthPower, earthPower));
                    
                    var mat = AlphaBlending(result, Position.X, Position.Y);
                    PreWrittenMats.Add(mat);
                }
            }

            Cv2.ImWrite(string.Format("__preframe_{0}.jpg", CurrentLayer), PreWrittenMats[0]);

            return this;
        }

        public Effector Shake(int shakeDegree, int shakeSpeed, int shakeCount)
        {
            PreWrittenMats = new List<Mat>();
            for (var i = 0; i < shakeCount; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    for (var k = 0; k < shakeDegree; k += shakeSpeed)
                    {
                        var result = _Rotate(Image, k);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                    }
                    for (var k = 0; k < 2; k++)
                    {
                        var result = _Rotate(Image, shakeDegree);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                        
                    }
                    
                    for (var k = shakeDegree; k > -shakeDegree; k -= shakeSpeed)
                    {
                        var result = _Rotate(Image, k);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                    }
                    
                    for (var k = -shakeDegree; k < 0; k += shakeSpeed)
                    {
                        var result = _Rotate(Image, k);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                    }
                    
                }
            }

            Cv2.ImWrite(string.Format("__preframe_{0}.jpg", CurrentLayer), PreWrittenMats[0]);

            return this;
        }

        public Effector Rotate(int degree, bool way, int speed)
        {

            Cv2.ImWrite(string.Format("first.jpg", CurrentLayer), Image);
            PreWrittenMats = new List<Mat>();
            
            if (way) //CounterClockwise
            {
                for (var i = 0; i > -degree; i -= speed)
                {
                    var result = _Rotate(Image,i);
                    var mat = AlphaBlending(result, Position.X, Position.Y);
                    PreWrittenMats.Add(mat);
                }
            }
            else //Clockwise
            {
                for (var i = 0; i < degree; i += speed)
                {
                    var result = _Rotate(Image, i);
                    var mat = AlphaBlending(result, Position.X, Position.Y);
                    PreWrittenMats.Add(mat);
                }
            }
            PreWrittenMats.Add(_Rotate(Image, degree));

            return this;
        }

        public Effector FullRotate(int speed, bool way, int count)
        {
            PreWrittenMats = new List<Mat>();

            if (way) //CounterClockwise
            {
                for (var i = 0; i < count; i++)
                    for (var j = 360; j > 0; j -= speed)
                    {
                        var result = _Rotate(Image, j);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                    }
            }
            else //Clockwise
            {
                for (var i = 0; i < count; i++)
                    for (var j = 0; j < 360; j += speed)
                    {
                        var result = _Rotate(Image, j);
                        var mat = AlphaBlending(result, Position.X, Position.Y);
                        PreWrittenMats.Add(mat);
                    }
            }
                speed = -speed;

            

            Cv2.ImWrite(string.Format("__preframe_{0}.jpg", CurrentLayer), PreWrittenMats[0]);

            return this;
        }

        public Effector Transition(int xStart, int yStart, int time, int xDes, int yDes)
        {
            PreWrittenMats = new List<Mat>();
            int frame = time * (int)Framerate;
            for (var i = 0; i < frame; i++)
            {
                var result = _Translate(Image, xStart, -yStart);
                var mat = AlphaBlending(result, Position.X, Position.Y);
                PreWrittenMats.Add(mat);
                xStart = xStart + xDes/time;
                yStart = yStart + yDes/time;
            }

            return this;
        }
    }
}
