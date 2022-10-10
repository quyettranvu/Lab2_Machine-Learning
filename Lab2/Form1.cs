using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

using AForge.Video;
using AForge.Video.DirectShow;

namespace Lab2
{
    public partial class Form1 : Form
    {
        /*******************************Member variables**************************************/
        private bool blnFirstTimeInResizeEvent = true; //used to throw out first time in form resize event
         
        //variables to resize original image box to form
        private int intOrigFormWidth, intOrigFormHeight, intOrigImageBoxWidth,intOrigImageBoxHeight;

        private Capture capWebcam; //Capture Object for webcam
        private bool blnWebcamCapturingInProcess = false; //keep track of SURF function added to task

        private Image<Bgr, Byte> imgSceneColor = null; //original image scene in color
        private Image<Bgr, Byte> imgToFindColor = null; //original image to find in color
        private Image<Bgr, Byte> imgCopyOfImageToFindWithBorder = null; //use as a copy of image so we can draw a border on this image without altering original iamge

        private bool blnImageSceneLoaded = false; //flag to track whether image scene is loaded successfully
        private bool blnImageToFindLoaded = false; //flag to track whether an image to find is loaded successfully

        private Image<Bgr, Byte> imgResult = null;  //result image

        private Bgr bgrKeyPointsColor = new Bgr(Color.Blue); //color to draw keypoint on result image
        private Bgr bgrMatchingLinesColor = new Bgr(Color.LightPink); //color to draw lines on result image
        private Bgr bgrFoundImageColor = new Bgr(Color.Red); //color to draw around found image in scene portion of result image

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); //stopwatch to watch processing time

        public Form1()
        {
            InitializeComponent();
        }


        /******************************Main Functions*****************************************/
        //Form_resize
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (blnFirstTimeInResizeEvent)                     //if first time in resize event update flag variable
                blnFirstTimeInResizeEvent = false;
            else 
            {                                                   //after first time in resize event resize imageBox to form
                imageBox1.Width = this.Width - (intOrigFormWidth - intOrigImageBoxWidth);
                imageBox1.Height = this.Height - (intOrigImageBoxHeight - intOrigImageBoxHeight);
            }
        }

        //Load Image File
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton2.Checked == true)
            {
                //if webcam is in process then remove event from application idle and update flag variable
                if(blnWebcamCapturingInProcess == true)
                {
                    Application.Idle -= new EventHandler(this.PerformSURFDetectionAndUpdateGUI);
                    blnWebcamCapturingInProcess = false;
                }

                /*reset class variables*/
                imgSceneColor = null;
                imgToFindColor = null;
                imgCopyOfImageToFindWithBorder = null;
                imgResult = null;
                blnImageSceneLoaded = false;
                blnImageToFindLoaded = false;

                /*reset form*/
                textBox2.Text = "";
                textBox3.Text = "";
                imageBox1.Image = null;

                this.Text = "Instructions: Use ... buttons to choose both image files then press Perform Surf Detection button";
                button3.Text = "Perform SURF Detection";
                imageBox1.Image = null;  /*make image box blank*/

                /*show controls that are used for still images*/
                label4.Visible = true;
                label5.Visible = true;
                textBox2.Visible = true;
                textBox3.Visible = true;
                button1.Visible = true;
                button2.Visible = true;
            }
        }

        //Turn on webcam
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            /*if webcam was chosen reset class variables*/
            if(radioButton3.Checked ==true)
            {
                imgSceneColor = null;
                imgToFindColor = null;
                imgCopyOfImageToFindWithBorder = null;
                imgResult = null;
                blnImageSceneLoaded = false;
                blnImageToFindLoaded = false;

                textBox2.Text = "";      /*reset form*/
                textBox3.Text = "";
                imageBox1.Image = null;


                /*Create instance of VideoCapture then check if not successfully show out error*/
                try
                {
                  capWebcam = new Emgu.CV.Capture();
                }
                catch (NullReferenceException excpt)
                {
                  MessageBox.Show(excpt.Message);
                }

                this.Text = "Instruction: Hold Image to track up before Camera then press get image to track";
                button3.Text = "get image to track";
                imgToFindColor = null;

                /*Add event for SURF Detection and update flag variables since we already had image on webcam*/
                Application.Idle += new EventHandler(this.PerformSURFDetectionAndUpdateGUI);
                blnWebcamCapturingInProcess = true;
                //Timer timer = new Timer();
                //timer.Interval = Convert.ToInt32(TimeSpan.FromMilliseconds(1000 / 60).TotalMilliseconds);
                //timer.Tick += PerformSURFDetectionAndUpdateGUI;
                //timer.Start();

                label4.Visible = false;  
                label5.Visible = false;
                textBox2.Visible = false;
                textBox3.Visible = false;
                button1.Visible = false;
                button2.Visible = false;
            }
        }

        //Find Scene Image
        private void button1_Click(object sender, EventArgs e)
        {
            /*bring up image scene file dialog box*/
            OpenFileDialog v1 = new OpenFileDialog();
            v1.Filter ="Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;...";

            /*If OK or Yes was chosen*/
            if (v1.ShowDialog() == DialogResult.OK || v1.ShowDialog() == DialogResult.Yes)
            {
                textBox2.Text = v1.FileName;
            }
            else return;

            try
            {
                imgSceneColor = new Image<Bgr, Byte>(textBox2.Text); //try to load to find image
            }
            catch(Exception ex)
            {
                this.Text = ex.Message;
                return;
            }

            /*if we get here, to find image loaded successfully, update member variable*/
            blnImageSceneLoaded = true;


            if (blnImageToFindLoaded == false)     // 'if to find image has not been loaded yet
            {
                imageBox1.Image = imgSceneColor;   // show image scene we just loaded on image box
            }
            //if to find image has already been loaded
            else
            {
                imageBox1.Image = imgSceneColor.ConcateHorizontal(imgCopyOfImageToFindWithBorder); //(thực hiện nối hình ảnh) concatenate image to find with border and scene image, show result on image box
            }                          
        }

        //find Object Image
        private void button2_Click(object sender, EventArgs e)
        {
            /*bring up image scene file dialog box*/
            OpenFileDialog v1 = new OpenFileDialog();
            v1.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;...";

            /*If OK or Yes was chosen*/
            if (v1.ShowDialog() == DialogResult.OK || v1.ShowDialog() == DialogResult.Yes)
            {
                textBox3.Text = v1.FileName;
            }
            else return;

            try
            {
                imgToFindColor = new Image<Bgr, Byte>(textBox3.Text); //try to load to find image
            }
            catch (Exception ex)
            {
                this.Text = ex.Message;
                return;
            }

            /*if we get here, to find image loaded successfully, update member variable*/
            blnImageToFindLoaded = true;

            //make a copy of the image to find, so we can draw on the copy, therefore not changing the original image to find
            imgCopyOfImageToFindWithBorder = imgToFindColor.Copy();

            //draw 2 pixel wide border around the copy of the image to find, use same color as box for found image
            imgCopyOfImageToFindWithBorder.Draw(new Rectangle(1, 1, imgCopyOfImageToFindWithBorder.Width - 3, imgCopyOfImageToFindWithBorder.Height - 3), bgrFoundImageColor, 2);

            //if scene image is already loaded
            if (blnImageSceneLoaded == true)                                               
            {
                imageBox1.Image = imgSceneColor.ConcateHorizontal(imgCopyOfImageToFindWithBorder); //concatenate to find image with border to scene image, show result on image box
            }
            else
            {
                imageBox1.Image = imgCopyOfImageToFindWithBorder; //show to find image border we just loaded on image box
            }
        }

        //Text Change Image Scene
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            /*Move caret(dấu mũ) to the end of text box so file name is visible rather than file extension*/
            textBox2.SelectionStart = textBox2.Text.Length;
        }


        //Text Change Object Image
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            textBox3.SelectionStart = textBox3.Text.Length;
        }

        //Perform Surf Detection
        private void button3_Click(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                /*check whether something is entered in each image*/
                if ((textBox2.Text != String.Empty) && (textBox3.Text != String.Empty))
                {
                    PerformSURFDetectionAndUpdateGUI(new object(), new EventArgs());
                }
                else
                {   /*remind user to choose both image files*/
                    this.Text = "choose image files first then choose Perform SURF Detection button";
                }
            }
            else if (radioButton3.Checked == true)
            {
                /*use most recent image from webcam, which will be in imgSceneColor, then shrink and save as new image to track*/
                imgToFindColor = capWebcam.QueryFrame().Resize(320, 240, INTER.CV_INTER_CUBIC, true);
                this.Text = "Instructions: to update image to track, hold image up to camera, then press perform update image to track";
                button3.Text = "update image to track";
            }
        }

        //Draw markers/keypoints
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            /*if draw keypoints was just unchecked then uncheck draw matching lines and disable this checkbox*/
            if (checkBox1.Checked == false)
            {
                checkBox2.Checked = false;
                checkBox2.Enabled = false;
            }
            else if (checkBox1.Checked == true)
            {
                    checkBox2.Enabled = true;
            }

            /*if using image file call SURF button click event to update image for draw key points check box change*/
            if (radioButton2.Checked==true)
            {
                button3_Click(new Object(), new EventArgs());
            }
        }

        //Connect keypoints lines
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton2.Checked==true)
            {
                button3_Click(new Object(), new EventArgs()); /*call SURF button click event to update image for draw matching lines check box change*/
            }
        }

        public void PerformSURFDetectionAndUpdateGUI(object sender, EventArgs e)
        {
            //if still getting image from file
            if(radioButton2.Checked==true)
            {
                //if either flag variable indicates we do not have image or either is null
                if (blnImageSceneLoaded == false || blnImageToFindLoaded == false || imgSceneColor == null || imgToFindColor == null)
                {
                    this.Text = "Either or both images are not loaded or null, please choose both images before performing SURF Detection";
                    return;
                }

                this.Text = "Processing, please wait...."; 
                Application.DoEvents();    /*show processing on title bar, then update form and restart*/
                stopwatch.Restart();
            }
            else   //else if getting image from webcam
            {
                if (radioButton3.Checked == true)
                {
                    try
                    {
                        imgSceneColor = capWebcam.QueryFrame(); //get next frame
                    }
                    catch (Exception ex)
                    {
                        this.Text = ex.Message;
                        return;
                    }
                }

                if(imgSceneColor ==null)
                {
                    this.Text = "Error, imgSceneColor is nothing!!!";
                    return;
                }

                /*If we dont have an image to track then show image Scene on the imageBox*/
                if(imgToFindColor ==null)
                {
                    imageBox1.Image = imgSceneColor;
                }
            }

            /*From here both color images are good, we are gonna deal with the SURF detection*/
            //instantiate SURF object, params are threshold and extended flag
            SURFDetector surfDetector = new SURFDetector(500, false);

            //grayscale image scene and image to find
            Image<Gray, Byte> imgSceneGray = null;
            Image<Gray, Byte> imgToFindGray = null;

            //vector of key points in scene and in image to find; matrix of descriptors 
            VectorOfKeyPoint vkpSceneKeyPoints, vkpToFindKeyPoints;
            Matrix<Single> mtxSceneDescriptors, mtxToFindDescriptors;

            Matrix<Int32> mtxMatchIndices; //matrix of descriptor indices, will result from training descriptors
            Matrix<Single> mtxDistance;    //matrix of distance values, will result from training descriptors
            Matrix<Byte> mtxMask;          //both input and output to function VoteForUniqueness(), indicates which row is valid for the matches

            BruteForceMatcher<Single> bruteForceMatcher;  //for each descriptor in the first set, this matcher finds the closest descriptor in the second set by trying each one
            HomographyMatrix homographyMatrix = null;     //used for ProjectPoints() function to set location of found image in scene image

            int intKNumNearestNeighbors = 2;              //k, number of nearest neighbors to search for
            double dblUniquenessThreshold = 0.8;          //the distance difference ratio for a match to be considered unique

            int intNumNonZeroElements;        //used as return value for number of non-zero elements both in matrix mask, and also from call to GetHomographyMatrixFromMatchedFeatures()
            double dblScaleIncrement = 1.5;   //determines the difference in scale for neighboring bins
            int intRotationBins = 20;         //number of bins for rotation out of 360 deg, for example if set to 20, each bin covers 18 deg (20 * 18 = 360)
            double dblRansacReprojectionThreshold = 2.0;  //for use with GetHomographyMatrixFromMatchedFeatures(), the maximum allowed reprojection error to treat a point pair as an inlier

            Rectangle rectImageToFind;   //rectangle encompassing the entire image to find
            PointF[] ptfPointsF;         //4 points defining box around location of found image in scene in float type
            Point[] ptPoints;            //4 points defining box around location of found image in scene in interger type

            //Convert both scene image and image to find to gray scale
            imgSceneGray = imgSceneColor.Convert<Gray, Byte>();
            imgToFindGray = imgToFindColor.Convert<Gray, Byte>();

            vkpSceneKeyPoints = surfDetector.DetectKeyPointsRaw(imgSceneGray, null); //detect the key points in the scene
            mtxSceneDescriptors = surfDetector.ComputeDescriptorsRaw(imgSceneGray, null,vkpSceneKeyPoints);  //compute scene descriptor

            vkpToFindKeyPoints = surfDetector.DetectKeyPointsRaw(imgToFindGray, null); //detect the key points in the image to find
            mtxToFindDescriptors = surfDetector.ComputeDescriptorsRaw(imgToFindGray, null,vkpToFindKeyPoints); //compute to find descriptor

            bruteForceMatcher = new BruteForceMatcher<Single>(DistanceType.L2); //instantiate brute force matcher object
            bruteForceMatcher.Add(mtxToFindDescriptors);  //add matrix for to find descriptors to brute force matcher

            mtxMatchIndices = new Matrix<int>(mtxSceneDescriptors.Rows,intKNumNearestNeighbors); //instantiate the indices matrix, params are rows and columns
            mtxDistance = new Matrix<Single>(mtxSceneDescriptors.Rows,intKNumNearestNeighbors);  //instantiate the distance matrix, params are rows and columns

            bruteForceMatcher.KnnMatch(mtxSceneDescriptors, mtxMatchIndices, mtxDistance,intKNumNearestNeighbors, null);  //find the k-nearest match

            mtxMask = new Matrix<Byte>(mtxDistance.Rows, 1); //instantiate matrix mask
            mtxMask.SetValue(255);                           //set value of all elements in mask matrix

            Features2DToolbox.VoteForUniqueness(mtxDistance, dblUniquenessThreshold,mtxMask);  //filter the matched features, such that if a match is not unique, it is rejected
            intNumNonZeroElements = CvInvoke.cvCountNonZero(mtxMask);   //get number of non-zero elements in mask matrix

            if (intNumNonZeroElements >= 4)
            {
                //eliminate the matched features whose scale and rotation do not agree with the majority's scale and rotation
                intNumNonZeroElements = Features2DToolbox.VoteForSizeAndOrientation(vkpToFindKeyPoints, vkpSceneKeyPoints,mtxMatchIndices, mtxMask, dblScaleIncrement, intRotationBins);
                if (intNumNonZeroElements >= 4)
                {
                    //get the homography matrix using RANSAC (RANdom SAmple Consensus)
                    homographyMatrix = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(vkpToFindKeyPoints,vkpSceneKeyPoints, mtxMatchIndices, mtxMask, dblRansacReprojectionThreshold);
                }
            }

            imgCopyOfImageToFindWithBorder = imgToFindColor.Copy();

            //draw a 2 pixel wide border around the copy of the image to find, use same color as box for found image
            imgCopyOfImageToFindWithBorder.Draw(new Rectangle(1, 1,imgCopyOfImageToFindWithBorder.Width - 3, imgCopyOfImageToFindWithBorder.Height -3), bgrFoundImageColor, 2);

            if (checkBox1.Checked == true && checkBox2.Checked == true)
            {
                //if drawing both key points & matching lines on result image use DrawMatches
                imgResult = Features2DToolbox.DrawMatches(imgCopyOfImageToFindWithBorder, vkpToFindKeyPoints, imgSceneColor, vkpSceneKeyPoints, mtxMatchIndices, bgrMatchingLinesColor, bgrKeyPointsColor,mtxMask,Features2DToolbox.KeypointDrawType.DEFAULT);
            }
            else if (checkBox1.Checked == true && checkBox2.Checked == false)
            {
                //if only drawing keypoints on result image then concatenate copy of image to find onto result image
                imgResult = Features2DToolbox.DrawKeypoints(imgSceneColor,vkpSceneKeyPoints, bgrKeyPointsColor,Features2DToolbox.KeypointDrawType.DEFAULT);
                imgCopyOfImageToFindWithBorder =Features2DToolbox.DrawKeypoints(imgCopyOfImageToFindWithBorder,vkpToFindKeyPoints, bgrKeyPointsColor, Features2DToolbox.KeypointDrawType.DEFAULT);
                imgResult = imgResult.ConcateHorizontal(imgCopyOfImageToFindWithBorder);
            }
            else if (checkBox1.Checked == false && checkBox2.Checked == false)
            {
                imgResult = imgSceneColor;
                imgResult = imgResult.ConcateHorizontal(imgCopyOfImageToFindWithBorder);
            }

            //check homographyMatrix not null since in this next portion we draw a border on the scene portion of the result image, around where the found object is located
            if (homographyMatrix != null)
            {
                rectImageToFind = new Rectangle(0, 0, imgToFindGray.Width,imgToFindGray.Height);
                ptfPointsF = new PointF[]
                {
                      new PointF(rectImageToFind.Left, rectImageToFind.Top),
                      new PointF(rectImageToFind.Right, rectImageToFind.Top),
                      new PointF(rectImageToFind.Right, rectImageToFind.Bottom),
                      new PointF(rectImageToFind.Left, rectImageToFind.Bottom)
                };

                //Function ProjectPoints will set ptfPointsF to location of a box in the scene portion of the result image, where the box surrounds the image we are looking for
                homographyMatrix.ProjectPoints(ptfPointsF);
                ptPoints = new Point[]
                {
                     Point.Round(ptfPointsF[0]),
                     Point.Round(ptfPointsF[1]),
                     Point.Round(ptfPointsF[2]),
                     Point.Round(ptfPointsF[3]),
                };

                //draw border around found image in scene portion of result image
                imgResult.DrawPolyline(ptPoints, true, bgrFoundImageColor, 2);
            }

            imageBox1.Image = imgResult;
            //if using image from file(user change another image) then stop watch and show on title bar
            if (radioButton2.Checked == true)
            {
                stopwatch.Stop();
                this.Text = "processing time = " + stopwatch.Elapsed.TotalSeconds.ToString();
            }

        }
    }
}
