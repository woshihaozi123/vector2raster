using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.IO;

namespace vector2raster1
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string tifFile="I:\\shp\\2.tif";
            string[] strs={"I:\\shp\\3.shp","I:\\shp\\11.shp","I:\\shp\\17.shp"};
            string shpcombfile = "I:\\shp\\result.shp";//;strs[0].Substring(0, strs[0].LastIndexOf("\\")) + "\\result.shp";
                //"I:\\shp\\result.shp";
            string fieldName = "grid_code";
            int cellSize=300;
            Vector2Raster myvr = new Vector2Raster();
            myvr.InitinalGdal();
            myvr.VectorCombine(strs, shpcombfile);
            myvr.Rasterize(shpcombfile, tifFile, fieldName, cellSize);
            Console.ReadKey();
            
        }

       


   }
    class Vector2Raster
    { 
         OSGeo.OGR.Driver oDerive;
         Layer olayer;
         Dataset OutputDS;
        /// <summary>
        /// 初始化GDAL
        /// </summary>
        public void InitinalGdal() 
        {   //为了支持中文路径
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            //为了使属性表字段支持中文
            Gdal.SetConfigOption("SHAPE_ENCODING","");
            Gdal.AllRegister();
            Ogr.RegisterAll();

            oDerive=Ogr.GetDriverByName("ESRI Shapefile");
            if(oDerive==null)
            {
                System.Console.WriteLine("oDerive error");
             }           
          
        }
        public  Layer  GetShpLayer(string sFileName)
        {
             Layer olayer;
             if(oDerive==null)
            {
                System.Console.WriteLine("oDerive error");
                 
             }
            DataSource ds=oDerive.Open(sFileName,1);
            if(ds==null)
            {
                System.Console.WriteLine("ds open error");
                
            }
              olayer=ds.GetLayerByIndex(0);
             if (olayer == null)
             {
                 System.Console.WriteLine("layer open error");
                ds.Dispose();  
               
            }
             return olayer;
        
        }

        //public bool CreateRaster(string filepathout,int m_XSize,int m_YSize) 
        //{
        //    //在GDAL中创建影像,先需要明确待创建影像的格式,并获取到该影像格式的驱动  
        //    OSGeo.GDAL.Driver driver = Gdal.GetDriverByName("GTiff");
        //    //调用Creat函数创建影像  
        //    OutputDS = driver.Create(filepathout, m_XSize, m_YSize, 1, DataType.GDT_Int32, null);
        //    //设置影像属性  
        //    // OutputDS.SetGeoTransform(m_adfGeoTransform); //影像转换参数  
        //   //  OutputDS.SetProjection(m_DEMDataSet.GetProjection()); //投影  
        //    //将影像数据写入内存  
        //  //  OutputDS.GetRasterBand(1).WriteRaster(0, 0, m_XSize, m_YSize, OutputData, m_XSize, m_YSize, 0, 0);
        //    OutputDS.GetRasterBand(1).FlushCache();
        //    OutputDS.FlushCache();
            
        //    return true;
        //}
        
        /// <summary>
        /// 矢量转栅格
        /// </summary>
        /// <param name="inputFeature"></param>
        /// <param name="outRaster"></param>
        /// <param name="fieldName"></param>
        /// <param name="cellSize"></param>
        public void Rasterize(string inputFeature, string outRaster, string fieldName, int cellSize)
        {
            OSGeo.OGR.Driver oDerive;
            oDerive = Ogr.GetDriverByName("ESRI Shapefile");
            //Define pixel_size and NoData value of new raster  
            int rasterCellSize = cellSize;
            const double noDataValue = -9999;
            string outputRasterFile = outRaster;
            //Register the vector drivers  
            Ogr.RegisterAll();
            //Reading the vector data  
            DataSource dataSource = oDerive.Open(inputFeature, 0);
            Layer layer = dataSource.GetLayerByIndex(0);
            Envelope envelope = new Envelope();
            layer.GetExtent(envelope, 0);
            //Compute the out raster cell resolutions  
            int x_res = Convert.ToInt32((envelope.MaxX - envelope.MinX) / rasterCellSize);
            int y_res = Convert.ToInt32((envelope.MaxY - envelope.MinY) / rasterCellSize);
            //Console.WriteLine("Extent: " + envelope.MaxX + " " + envelope.MinX + " " + envelope.MaxY + " " + envelope.MinY);
           // Console.WriteLine("X resolution: " + x_res);
           // Console.WriteLine("X resolution: " + y_res);
            //Register the raster drivers  
            Gdal.AllRegister();
            //Check if output raster exists & delete (optional)  
            if (File.Exists(outputRasterFile))
            {
                File.Delete(outputRasterFile);
            }
            //Create new tiff   
            OSGeo.GDAL.Driver outputDriver = Gdal.GetDriverByName("GTiff");
            Dataset outputDataset = outputDriver.Create(outputRasterFile, x_res, y_res, 1, DataType.GDT_Float64, null);
            //Extrac srs from input feature   
            string inputShapeSrs;
            OSGeo.OSR.SpatialReference spatialRefrence = layer.GetSpatialRef();
            spatialRefrence.ExportToWkt(out inputShapeSrs);
            //Assign input feature srs to outpur raster  
            outputDataset.SetProjection(inputShapeSrs);
            //Geotransform  
            double[] argin = new double[] { envelope.MinX, rasterCellSize, 0, envelope.MaxY, 0, -rasterCellSize };
            outputDataset.SetGeoTransform(argin);
            //Set no data  
            Band band = outputDataset.GetRasterBand(1);
            band.SetNoDataValue(noDataValue);
            //close tiff  
            outputDataset.FlushCache();
            outputDataset.Dispose();
            //Feature to raster rasterize layer options  
            //No of bands (1)  
            int[] bandlist = new int[] { 1 };
            //Values to be burn on raster (10.0)  
            double[] burnValues = new double[] { 10.0 };
            Dataset myDataset = Gdal.Open(outputRasterFile, Access.GA_Update);
            //additional options  
            string[] rasterizeOptions;
            //rasterizeOptions = new string[] { "ALL_TOUCHED=TRUE", "ATTRIBUTE=" + fieldName }; //To set all touched pixels into raster pixel  
            rasterizeOptions = new string[] { "ALL_TOUCHED=TRUE", "ATTRIBUTE=grid_code" }; //{ "ATTRIBUTE=" + fieldName };
            //Rasterize layer  
            //Gdal.RasterizeLayer(myDataset, 1, bandlist, layer, IntPtr.Zero, IntPtr.Zero, 1, burnValues, null, null, null); // To burn the given burn values instead of feature attributes  
            Gdal.RasterizeLayer(myDataset, 1, bandlist, layer, IntPtr.Zero, IntPtr.Zero, 1, null, rasterizeOptions, new Gdal.GDALProgressFuncDelegate(ProgressFunc), "Raster conversion");
            myDataset.FlushCache();
            myDataset.Dispose();
        }
        private static int ProgressFunc(double complete, IntPtr message, IntPtr data)
        {
            Console.Write("Processing ... " + complete * 100 + "% Completed.");
            if (message != IntPtr.Zero)
            {
                Console.Write(" Message:" + System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message));
            }
            if (data != IntPtr.Zero)
            {
                Console.Write(" Data:" + System.Runtime.InteropServices.Marshal.PtrToStringAnsi(data));
            }
            Console.WriteLine("");
            return 1;
        }  
        public void VectorCombine(string[] inputFeatures,string resultfile) 
        {
              if(oDerive==null)
            {
                System.Console.WriteLine("oDerive error");
             }   
             //用此Driver创建Shape文件
            OSGeo.OGR.DataSource poDS;
            poDS = oDerive.CreateDataSource(resultfile, null);
            if (poDS == null)
            { 
                System.Console.WriteLine("ds creation error");
            }
            OSGeo.OGR.Layer poLayer;
            poLayer = poDS.CreateLayer("result", GetShpLayer(inputFeatures[0]).GetSpatialRef(), OSGeo.OGR.wkbGeometryType.wkbLineString, null);
            if (poLayer == null)
            {
                System.Console.WriteLine("layer creation error");
            }

           //for (int i = 0; i < inputFeatures.Length; i++)
           // {
           //     Layer olayer = GetShpLayer(inputFeatures[i]);
            //     poDS.FlushCache();
           //     poDS.CopyLayer(olayer, i.ToString(), null);
           // }
            // 下面创建属性表
            // 先创建一个叫FieldID的整型属性
            FieldDefn oFieldID = new FieldDefn("grid_code", FieldType.OFTInteger);
            poLayer.CreateField(oFieldID, 1);

            FeatureDefn oDefn = poLayer.GetLayerDefn();

            long fid = 0;
            List<int> codelist=new List<int>();
            for (int i = 0; i < inputFeatures.Length; i++)
            {
                Layer olayer = GetShpLayer(inputFeatures[i]);
                int featurenum = (int)olayer.GetFeatureCount(1);
                for (int j = 0; j < featurenum; j++)
                {


                    Feature feature = olayer.GetFeature(j);
                    codelist.Add(olayer.GetFeature(j).GetFieldAsInteger("grid_code"));

                    poLayer.CreateFeature(feature);
                    poLayer.CommitTransaction();


                }
            }
            poDS.FlushCache();

            Feature feature1 = poLayer.GetNextFeature();
            while (feature1 != null)
            {
                int id = (int)feature1.GetFID();
                feature1.SetField("grid_code", codelist[id]);

                poLayer.SetFeature(feature1);
                feature1 = poLayer.GetNextFeature();                
            }
          

            

            //for (int i = 0; i < inputFeatures.Length; i++)
            //{
            //    Layer olayer = GetShpLayer(inputFeatures[i]);
            //    int featurenum = (int)olayer.GetFeatureCount(1);
            //    for (int j = 0; j < featurenum; j++)
            //    {
            //        Console.WriteLine("{0}", olayer.GetFeature(j).GetFieldAsInteger("grid_code"));
            //        int grid_code = olayer.GetFeature(j).GetFieldAsInteger("grid_code");
            //        poLayer.GetFeature(fid).SetField("grid_code", grid_code);
            //        fid++;
            //    }
            //}
            poDS.Dispose();

        }


    }
}


