using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ConsumerResourceModelFramework;
//using WaterSimDCDC.America;
using WaterSimDCDC.Generic;
using WaterSimDCDC.Documentation;

//using WaterSimDCDC.Processes;

namespace WaterSimDCDC
{
    //
    // Build Notes:
    // ver 1.0.0 - First version
    // ver 1.1.1 - 02.28.16:  
    // ver 1.2.0 - 03.06.16: Added desalination (using epAugmented) (DESAL-Desal), Ag Growth Rate (AGGR-AgGrowthRate), and  void surfacelake() & Lake Water Management (LWM-d_lakeWaterControl)
    // ver 1.2.1 - 03.07.16: Changed Lake Water Management to work like the desalinaiton control.; added PST (policy Start Year); cleaned up WaterSimAmerica code
    //                       Ag growth, changed Lake Water Management to work as desalination, added "invokePolicies"
    // ver 1.2.2 - 03.10.16: Changed verbiage, added Power Energy and, I changed the way GPCD is calculated for each Consumer (i.e., now demand * 1000 / pop)
    // ver 1.3.0 - 04.03.16: I added Industrial water demand growth
    // ver 1.3.1 - 04.06.16: I changed Drought Override to a value of 60 (or 0.6 * flow)
    // ver 1.3.2 - 04.11.16: QUAY Brought in DAS Industrial Code and revised reading data.
    // ver 1.3.3 - 05.04.16: DAS - changed the reclaimed to add a constant amount each year based on the first year designation (to keep below maximum reclaimed by 2065)
    //                        also changed the default values to 0,1,2,3 to just make max reclaimed by 2065 with value 3 (Florida was my test case). 
    //                        Tested (and parameterized) on three states: 05.05.16
    // ver 1.3.5 - 05.06.16: DAS - added drought impacts on water demand for all sectors using  "public static double hyperbola(double droughtFactor)" and * modifyDemandCF();
    // ver 1.3.7 - 05.11.16: DAS - completely changed the controls for surface and groundwater management and in the America_Process.cs file
    //                       doubles to int's, 0 to 3 for the controls, 1 being baseline, 0 = less water, 2 and 3 = more water
    // ver 1.3.8 - 07.19.16  DAS added four state data for industry,added four state data for population, read industry data from a csv file, modified the population code (see notes)
    // ver 1.4.1 - 09.15.15  DAS We now read mean growth rates for population and industry from a file. We then use adapted algorithms that Ray wrote for agriculture
    //                       to model population and industry growth. I had to change the way the conservation parameter acts on these growth rates. I also deleted
    //                       code no longer being used. I have been testing Urban, Agriculture, PowerWater, and Industry for changes in population and changes in conservation.
    //                        I have one SigmaPlot program that highlights many of these tests (WAS_temp.jnb)
    // ver 2.0.0 - 12.08.16  I altered the parameter specs for the water management parameters to be consistent with the conservation management controls (i.e., same magnitude and function)
    //                       I altered the parameter specs for the reclaimed water management control to be consistent with the other controls
    // ver 2.0.1 - 01.31.17  SAL ( seti_Desalinization()) was incorrectly parameterized [was set at zero instead of 100]. And, I added the property Desalinization in place of using the constant
    //                      _desalinization.
    // ver 2.0.2 - 02.01.17 Fixed the Effluent control. I was compounding the values... instead, set to new each year. Line 1593 in WaterSimAmerica_2_* and,
    //                      it was being initialized to 100 instead of zero
    //=============================================================================================================================================================================================
    // Enums 
    //==========================================================
    //

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Values that represent providers. </summary>
    ///
    /// <remarks>   Mcquay, 1/12/2016. </remarks>
    ///-------------------------------------------------------------------------------------------------

    public enum eProvider
    {
         eArizona, eCalifornia, eColorado, eFlorida,  eIdaho, eIllinois, eMinnesota, eNevada, eNewMexico, eUtah, eWyoming, eBasin  

    }

    /// <summary>
    /// Provider class is one provider = State
    /// </summary>
    
    public static partial class ProviderClass
    {
        // Provider Routines, Constants and enums
        /// <summary>
        /// The last valid provider enum value
        /// </summary>
        /// <value>eProvider enum</value>
        public const eProvider LastProvider = eProvider.eWyoming;

        /// <summary>
        /// The first valid enum value
        /// </summary>
        /// <value>eProvider enum</value>
        public const eProvider FirstProvider = eProvider.eArizona;

        /// <summary>
        /// The Last valid Aggregator value
        /// </summary>
        /// <value>eProvider enum</value>
        public const eProvider LastAggregate = eProvider.eBasin;

        /// <summary>
        /// The number of valid Provider (eProvider) enum values for use with WaterSimModel and ProviderIntArray.
        /// </summary>
        /// <value>count of valid eProvider enums</value>
        /// <remarks>all providers after LastProvider are not considered one of the valid eProvider enum value</remarks>
        public const int NumberOfProviders = (int)LastProvider + 1;

        /// <summary>
        /// The number of valid Provide Aggregate (eProvider) enum values.
        /// </summary>
        /// <value>count of valid eProvider enums</value>
        /// <remarks>all providers after LastProvider are not considered one of the valid eProvider enum value</remarks>
        public static int NumberOfAggregates = ((int)LastAggregate - (int)LastProvider);

        internal const int TotalNumberOfProviderEnums = ((int)LastAggregate) + 1;

        private static string[] ProviderNameList = new string[TotalNumberOfProviderEnums]    {      
  
         "Arizona", "California", "Colorado", "Florida",  "Idaho", "Illinois",  "Minnesota", "Nevada", "NewMexico", "Utah", "Wyoming", "Colorado Basin"
             };

        private static string[] FieldNameList = new string[TotalNumberOfProviderEnums]  {      
 
            "AZ","CA","CO","FL","ID","IL","MN","NV","NM","UT","WY","CB"
            ,
           };

        private static eProvider[] BasinProviders = new eProvider[7] {
            eProvider.eArizona, eProvider.eCalifornia, eProvider.eColorado, eProvider.eNevada, eProvider.eNewMexico, eProvider.eUtah, eProvider.eWyoming
        };

        public static eProvider[] GetRegion(eProvider ep)
        {
            switch (ep)
            {
                case eProvider.eBasin:
                    return BasinProviders;
                default:
                    return null;
            }
        }

    }
    /////-------------------------------------------------------------------------------------------------
    /// <summary>   A model parameter. </summary>
    ///
    /// <remarks>   Mcquay, 1/12/2016. </remarks>
    ///-------------------------------------------------------------------------------------------------

    public static partial class eModelParam
    {
        //
        // Model Control
        public const int epState = 4;
        // Drivers
        public const int epPopulation = 5;
        public const int epGPCD_urban = 6;
        public const int epGPCD_ag = 7;
        public const int epGPCD_other = 8;

        // Policies
        public const int epPolicyStartYear = 11;
        public const int epUrbanWaterConservation = 12;
        public const int epAgWaterConservation = 13;
        public const int epPowerWaterConservation = 14;
        public const int epIndustrialWaterConservation = 15;
        public const int epGroundwaterManagement = 16;
        public const int epGroundwaterControl = 17;
        public const int epSurfaceWaterManagement = 18;
        public const int epSurfaceWaterControl = 19;
        public const int epReclainedWaterUse = 20;
        public const int epDroughtControl = 21;
        public const int epLakeWaterManagement = 22;
        public const int epAgriculturalGrowth = 23;

        // Externalities - Drivers
        public const int epPopGrowthAdjustment = 25;
        public const int epClimateDrought = 26;
        public const int epAgricultureProduction = 27;
//        public const int epAgricultureDemand = 253;

        //
        // Resources
        public const int epSurfaceFresh = 31;
       // public const int epSurfaceLake = 32;
        public const int epSurfaceSaline = 33;
        public const int epGroundwater = 34;
        public const int epEffluent = 35;
        public const int epAugmented = 36;
        public const int epTotalSupplies = 37;

        // Consumers
        public const int epUrban = 51;
        public const int epAgriculture = 52;
        public const int epIndustrial = 53;
        public const int epPower = 54;
        // Outcomes
        public const int epUrbanNet = 71;
        public const int epRuralNet = 72;
        public const int epAgricultureNet = 73;
        public const int epIndustrialNet = 74;
        public const int epPowerNet = 75;
        public const int epPowerEnergy = 76;
        //
        public const int epSurfaceFreshNet = 80;
        public const int epSurfaceSalineNet = 81;
        public const int epSurfaceLakeNet = 82;
        public const int epGroundwaterNet = 83;
        public const int epEffluentNet = 84;

        //
        // Sustainability Metrics
        public const int epSustainability_groundwater = 101;
        public const int epSustainability_surfacewater = 102;
        public const int epSustainability_personal = 103;
        public const int epSustainability_economy = 104;
        //
        // Other Metrics
        public const int epNetDemandDifference = 110;

        public const int epUrbanSurfacewater = 295;
        public const int epSurfaceLake = 296;
        public const int epPowerSurfacewater = 297;
        public const int epPowerSaline = 298;
        public const int epPowerGW = 299;

        // WEST MODEL
        
        public const int epP_Population =1005;
        public const int epP_GPCD_urban = 1006;
        public const int epP_GPCD_ag = 1007;
        public const int epP_GPCD_other = 1008;

        // Policies
        public const int epP_PolicyStartYear = 1011;
        public const int epP_UrbanWaterConservation = 1012;
        public const int epP_AgWaterConservation = 1013;
        public const int epP_PowerWaterConservation = 1014;
        public const int epP_IndustrialWaterConservation = 1015;
        public const int epP_GroundwaterManagement = 1016;
        public const int epP_GroundwaterControl = 1017;
        public const int epP_SurfaceWaterManagement = 1018;
        public const int epP_SurfaceWaterControl = 1019;
        public const int epP_ReclainedWaterUse = 1020;
        public const int epP_DroughtControl = 1021;
        public const int epP_LakeWaterManagement = 1022;
        public const int epP_AgriculturalGrowth = 1023;

        // Externalities - Drivers
        public const int epP_PopGrowthAdjustment = 1025;
        public const int epP_ClimateDrought = 1026;
        public const int epP_AgricultureProduction = 1027;
        //        public const int epP_AgricultureDemand = 253;

        //
        // Resources
        public const int epP_SurfaceFresh = 1031;
       // public const int epP_SurfaceLake = 1032;
        public const int epP_SurfaceSaline = 1033;
        public const int epP_Groundwater = 1034;
        public const int epP_Effluent = 1035;
        public const int epP_Augmented = 1036;
        public const int epP_TotalSupplies = 1037;

        // Consumers
        public const int epP_Urban = 1051;
        public const int epP_Agriculture = 1052;
        public const int epP_Industrial = 1053;
        public const int epP_Power = 1054;
        // Outcomes
        public const int epP_UrbanNet = 1071;
        public const int epP_RuralNet = 1072;
        public const int epP_AgricultureNet = 1073;
        public const int epP_IndustrialNet = 1074;
        public const int epP_PowerNet = 1075;
        public const int epP_PowerEnergy = 1076;
        //
        public const int epP_SurfaceFreshNet = 1080;
        public const int epP_SurfaceSalineNet = 1081;
        public const int epP_SurfaceLakeNet = 1082;
        public const int epP_GroundwaterNet = 1083;
        public const int epP_EffluentNet = 1084;

        //
        // Sustainability Metrics
        public const int epP_Sustainability_groundwater = 1101;
        public const int epP_Sustainability_surfacewater = 1102;
        public const int epP_Sustainability_personal = 1103;
        public const int epP_Sustainability_economy = 1104;
        //
        // Other Metrics
        public const int epP_NetDemandDifference = 1110;

        public const int epP_UrbanSurfacewater = 1295;
        public const int epP_SurfaceLake = 1296;
        public const int epP_PowerSurfacewater = 1297;
        public const int epP_PowerSaline = 1298;
        public const int epP_PowerGW = 1299;

        public const int epP_SUR_UD =1901;
        public const int epP_SUR_AD =1902;
        public const int epP_SUR_ID =1903;
        public const int epP_SUR_PD =1904;
        public const int epP_SURL_UD =1905;
        public const int epP_SURL_AD =1906;
        public const int epP_SURL_ID =1907;
        public const int epP_SURL_PD =1908;
        public const int epP_GW_UD =1909;
        public const int epP_GW_AD =1910;
        public const int epP_GW_ID =1911;
        public const int epP_GW_PD =1912;
        public const int epP_REC_UD =1913;
        public const int epP_REC_AD =1914;
        public const int epP_REC_ID =1915;
        public const int epP_REC_PD =1916;
        public const int epP_SAL_UD =1917;
        public const int epP_SAL_AD =1918;
        public const int epP_SAL_ID =1919;
        public const int epP_SAL_PD =1920;
    }
    
    //********************************************************************************
    //
    //
    // *******************************************************************************

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Manager for water simulations. </summary>
    ///
    /// <seealso cref="WaterSimDCDC.WaterSimManagerClass"/>
    ///-------------------------------------------------------------------------------------------------
    //
    
    //
    public partial class WaterSimManager :  WaterSimManagerClass
    {
//        protected WaterSimAmerica WSmith=null;
      

        protected WaterSimCRFModel WSmith = null;
        protected WaterSimModel WestModel = null;
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Constructor. </summary>
        ///
        /// <param name="DataDirectoryName">    Pathname of the data directory. </param>
        /// <param name="TempDirectoryName">    Pathname of the temp directory. </param>
        ///-------------------------------------------------------------------------------------------------
      //  WaterSimDCDC.Processes.AlterWaterManagementFeedbackProcess WM;

        public WaterSimManager(string DataDirectoryName, string TempDirectoryName)
            : base(DataDirectoryName, TempDirectoryName)
        {
            try
            {
                WestModel = new Generic.WaterSimModel(DataDirectory, TempDirectory);
      
                //WSmith = new WaterSimAmerica(DataDirectory, TempDirectory);
                WSmith = new WaterSimCRFModel(DataDirectory, TempDirectory);
             
                //  WM = new AlterWaterManagementFeedbackProcess("Alter Water Management");

                initialize_ModelParameters();
                //initialize_ExtendedDocumentation();
                initializeIndicators();
                //initializeFluxParameters();
            }
            catch (Exception ex)
            {
               // WSmith = null;
                WestModel = null;
                MessageBox.Show("WaterSim America was not created" + ex);
                throw new ArgumentNullException();
                
            }
        }
     
        //public CRF_Network_WaterSim_America TheCRFNetwork
        //public CRF_Network_WaterSim_America TheCRFNetwork


        public CRF_Unit_Network TheCRFNetwork
        {
            get { return WSmith.TheCRFNetwork; }
        }
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Simulation cleanup. </summary>
        ///
        /// <seealso cref="WaterSimDCDC.WaterSimManagerClass.Simulation_Cleanup()"/>
        ///-------------------------------------------------------------------------------------------------

        protected override void Simulation_Cleanup()
        {
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Check if Model Setup Correct. </summary>
        ///
        /// <returns>   true if it succeeds, false if it fails. </returns>
        ///
        /// <seealso cref="WaterSimDCDC.WaterSimManagerClass.ValidModelCheck()"/>
        ///-------------------------------------------------------------------------------------------------

        protected override bool ValidModelCheck()
        {
            //return (base.ValidModelCheck() && (WSmith != null));
            return (base.ValidModelCheck() && (WestModel != null));
        }
        //
        ////public WaterSimAmerica WaterSimAmerica
        //public WaterSimCRFModel WaterSimModel
        //{
        //    get { return WSmith; }
        //}

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Provides Access to the WaterSimAmerica Model. </summary>
        ///
        /// <value> The water simulation america model. </value>
        ///-------------------------------------------------------------------------------------------------

        //public WaterSimAmerica WaterSimAmericaModel
        //public WaterSimCRFModel WaterSimModel
        //{
        //    get { return WSmith; }
        //}

        public WaterSimModel WaterSimWestModel
        {
            get { return WestModel; }
        }
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Default maximum start year. </summary>
        ///
        /// <remarks>   Mcquay, 2/9/2016. </remarks>
        ///
        /// <returns>   An int. </returns>
        ///-------------------------------------------------------------------------------------------------

        public override int DefaultMaxStartYear()
        {
            return 2049;
        }

        public override int DefaultStartYear()
        {
            return 2015;
        }

        public override int DefaultStopYear()
        {
            return 2050;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Executes the year operation. </summary>
        ///
        /// <param name="year"> The year. </param>
        ///
        /// <returns>   true if it succeeds, false if it fails. </returns>
        ///
        /// <seealso cref="WaterSimDCDC.WaterSimManagerClass.runYear(int)"/>
        ///-------------------------------------------------------------------------------------------------

        protected override bool runYear(int year)
        {
            
            return (RunModelYear(year) == 0);
        }

 
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Executes the model year operation. </summary>
        ///
        /// <exception cref="NotImplementedException"> Thrown when the requested operation is
        ///     unimplemented. </exception>
        ///
        /// <param name="year"> The year. </param>
        ///
        /// <returns>   true if it succeeds, false if it fails. </returns>
        ///
        /// <seealso cref="WaterSimDCDC.WaterSimManagerClass.RunModelYear(int)"/>
        ///-------------------------------------------------------------------------------------------------
        // DAS 01.21.16 
        protected override int RunModelYear(int year)
        {
            // Quay Edit 2/9/16
            //int testrun = WSmith.runOneYear(year);

            // WATERSIMMODEL
            int  testrun = WestModel.runOneYear(year);

            return testrun;
         }

        public override void Simulation_Initialize()
        {
            base.Simulation_Initialize();
            //WSmith.ResetNetwork();
            resetManager();

            // WATERSIMMODEL
            WestModel.ResetNetwork();
        }
        // -------------------------------------------------------------------------------------------------------------------------
        //
        // =======================================
        protected override string GetModelVersion()
        {
            return "WSA.2.0.0";
        }
        // =======================================
        protected override void initialize_ExtendedDocumentation()
        {
            throw new NotImplementedException();
        }
        // =========================================
        //
        protected override void initialize_ModelParameters()
        {
            WaterSimManager WSim = (this as WaterSimManager);
            
          //  WSim.ProcessManager.AddProcess(WM);
            base.initialize_ModelParameters();
            ParameterManagerClass FPM = ParamManager;        
            Extended_Parameter_Documentation ExtendDoc = FPM.Extended;
            // =======================================================
             // Provider parameters
            // Inputs/Outputs
            //
            // Template(s)
            // ExtendDoc.Add(new WaterSimDescripItem(eModelParam.ep, "", "", "", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            // _pm.AddParameter(new ModelParameterClass(eModelParam.ep,"", "", rangeChecktype.rctCheckRange, 0, 0, geti_, seti_, RangeCheck.NoSpecialBase));
            //
            // NEW STUFF - State
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epState, "State Code", "ST", rangeChecktype.rctCheckRange, 0, 8, WSmith.geti_StateIndex, WSmith.seti_StateIndex, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epState, "The State Currently being Examined: one of five (Florida, Idaho, Illinois, Minnesota, Wyoming) in the initial work.", "", "The State Examined", "State", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // Drivers
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epPopulation, "Population Served", "POP", geti_Pop));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPopulation, "State Population People in any given year- we use an estimate of slope to project out to 2065", "ppl", "State Population (ppl)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //// Outputs
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epGPCD_urban, "Urban GPCD", "UGPCD", WSmith.geti_gpcd));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epGPCD_urban, "The GPCD (Gallons per Capita per Day) for delivered water for the Urban water sector.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epGPCD_ag, "Agricultural GPCD", "AGPCD", WSmith.geti_gpcdAg));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epGPCD_ag, "The GPCD (Gallons per Capita per Day) for delivered water for Agricultural Uses.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epGPCD_other, "Other GPCD: Power and Industry", "OGPCD", WSmith.geti_gpcdOther));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epGPCD_other, "The GPCD (Gallons per Capita per Day) for delivered water for Industrial Uses and Power Combined.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            ////

            // Resources
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceFresh, "Surface Water (Fresh)", "SUR", rangeChecktype.rctCheckRange, 0,20000 /* 50000000 */, WSmith.geti_SurfaceWaterFresh, WSmith.seti_SurfaceWaterFresh, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epSurfaceFresh, "Fresh Water Deliveries from Surface Sources; this is total fresh water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceFreshNet, "Surface Water (Fresh) Net", "SURN", WSmith.geti_SurfaceWaterFreshNet));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceSaline, "Surface Water (Saline)", "SAL", rangeChecktype.rctCheckRange, 0,20000 /* 50000000 */, WSmith.geti_SurfaceWaterSaline, WSmith.seti_SurfaceWaterSaline, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epSurfaceSaline, "Saline Water Deliveries from Surface Sources; this is total saline water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceSalineNet, "Surface Water (Saline) Net", "SALN", WSmith.geti_SurfaceWaterSalineNet));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epGroundwater, "Groundwater (Fresh)", "GW", rangeChecktype.rctCheckRange, 0,20000 /* 500000008*/, WSmith.geti_Groundwater, WSmith.seti_Groundwater, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epGroundwater, "Fresh Water Deliveries from Pumped Groundwater; this is total water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epGroundwaterNet, "Groundwater (Fresh) Net", "GWN", WSmith.geti_GroundwaterNet));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epEffluent, "Effluent (Reclaimed)", "REC", rangeChecktype.rctCheckRange, 0, 20000 /*50000000*/, WSmith.geti_Effluent, WSmith.seti_Effluent, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epEffluent, "Effluent (reclaimed) Water Deliveries from Waste Water Treatment Plants; total withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epEffluentNet, "Effluent (Reclaimed) Net", "RECN", WSmith.geti_EffluentNet));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceLake, "Surface Lake Water", "SURL", rangeChecktype.rctCheckRange, 0, 20000, WSmith.geti_SurfaceLake, WSmith.seti_SurfaceLake, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epSurfaceLake, "Surface Lake Water", "mgd", "Million Gallons Per Day", "Surface Lake Water", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceLakeNet, "Surface Lake Water Net", "SURLN", WSmith.geti_SurfaceLakeNet));

            //
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epTotalSupplies, "Total Supplies", "TS", WSmith.geti_TotalSupplies));


            //// CONSUMERS
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epUrban, "Urban Demand", "UD", rangeChecktype.rctCheckRange, 0,30000 /*50000000*/, WSmith.geti_Urban, WSmith.seti_Urban, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epUrban, "Urban Water Demand", "MGD ", "Million Gallons per Day", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epUrbanNet, "Urban Demand (Net)", "UDN", WSmith.geti_Urban_Net));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epUrbanNet, "Urban (residential) Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAgriculture, "Agriculture Demand", "AD", rangeChecktype.rctCheckRange, 0,30000 /*50000000*/, WSmith.geti_Agriculture, WSmith.seti_Agriculture, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAgriculture, "Agriculture Water Demand; total withdrawals.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAgricultureNet, "Agriculture Demand (Net)", "ADN", WSmith.geti_Agriculture_Net));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAgricultureNet, "Agricultural Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epIndustrial, "Industrial Demand", "ID", rangeChecktype.rctCheckRange, 0,30000 /* 50000000*/, WSmith.geti_Industrial, WSmith.seti_Industrial, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epIndustrial, "Industrial Water Demand; total withdrawals. Water used for industries such as steel, chemical, paper, and petroleum refining. ", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epIndustrialNet, "Industrial Demand (Net)", "IDN", WSmith.geti_Industrial_Net));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epIndustrialNet, "Industrial Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epPower, "Power Demand", "PD", rangeChecktype.rctCheckRange, 0, 30000 /*50000000*/, WSmith.geti_PowerWater, WSmith.seti_PowerWater, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPower, "Water Use by Power: total withdrawals. Water used in the process of generating electricity with steam-driven turbine generators [Thermoelectric power, subcategories by cooling-system type (once-through, closed-loop/recirculation)].", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epPowerNet, "Power Demand (Net)", "PDN", WSmith.geti_PowerWater_Net));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPowerNet, "Power Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            // //
            //   _pm.AddParameter(new ModelParameterClass(eModelParam.epPowerEnergy, "Power Produced", "PE", WSmith.geti_PowerEnergy));
            ////
            //   _pm.AddParameter(new ModelParameterClass(eModelParam.epNetDemandDifference, "Net Demand Difference", "DDIF", rangeChecktype.rctCheckRange, 0, 100 /*50000000*/, WSmith.geti_NetDemandDifference, null, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epNetDemandDifference, "The ratio of net demand to total demand for all consumers; ", "% ", "Percent (%)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));


             //
            // Controls - Policy
            
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epUrbanWaterConservation, "Water Conservation (urban & rural)", "UCON", rangeChecktype.rctCheckRange, 50, 100, WSmith.geti_UrbanConservation, WSmith.seti_UrbanConservation, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epUrbanWaterConservation, "Urban Water Conservation: reduction in annual water use.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAgWaterConservation, "Ag Water Conservation", "ACON", rangeChecktype.rctCheckRange, 50, 100, WSmith.geti_AgConservation, WSmith.seti_AgConservation, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAgWaterConservation, "Agricultural Water Conservation: reduction in annual water used by the Ag sector.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epPowerWaterConservation, "Power Water Conservation", "PCON", rangeChecktype.rctCheckRange, 50, 100, WSmith.geti_PowerConservation, WSmith.seti_PowerConservation, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPowerWaterConservation, "Power Water Conservation: reduction in annual water use for Thermoelectric power generation.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));
            //   //
            //   _pm.AddParameter(new ModelParameterClass(eModelParam.epIndustrialWaterConservation, "Industrial Water Conservation", "ICON", rangeChecktype.rctCheckRange, 50, 100, WSmith.geti_IndustryConservation, WSmith.seti_IndustryConservation, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epIndustrialWaterConservation, "Industrial Water Conservation: reduction in annual water use for Industry.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));


            //// Index Values
            //   _pm.AddParameter(new ModelParameterClass(eModelParam.epSurfaceWaterManagement, "Use More Surface Water", "SWM", rangeChecktype.rctCheckRange, 80, 150, WSmith.geti_SurfaceWaterControl, WSmith.seti_SurfaceWaterControl, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epSurfaceWaterManagement, "Controls Scenario Chosen for alteration in surface water supply: increased surface water withdrawals.", "", "Alteration in Available Surface Water", "", new string[4] { "Less", "None", "Med", "High" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));
            //// 0=20% decrease, 1=contenporary, 2=20% increase, 3 = 40% increase in river water
            //   _pm.AddParameter(new ModelParameterClass(eModelParam.epGroundwaterManagement, "Change Groundwater Use", "GWM", rangeChecktype.rctCheckRange, 80, 150, WSmith.geti_GroundwaterControl, WSmith.seti_GroundwaterControl, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epGroundwaterManagement, "Controls Scenario Chosen for alteration in groundwater supplies: increased or decreased groundwater withdrawals.", "", "Alteration in Groundwater Used", "", new string[4] { "Less", "None", "More", "Most" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epReclainedWaterUse, "Use Reclaimed Water", "RECM", rangeChecktype.rctNoRangeCheck, 0, 100, WSmith.geti_ReclaimedWaterManagement, WSmith.seti_ReclaimedWaterManagement, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epReclainedWaterUse, "Alteration in reclaimed (effluent) supplies: increased effluent withdrawals.", "", "% of indoor water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 0, 33, 66, 100}, new ModelParameterGroupClass[] { }));
            ////
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epLakeWaterManagement, "Increase Lake Water use", "LWM", rangeChecktype.rctCheckRange, 80, 150, WSmith.geti_LakeWaterManagement, WSmith.seti_LakeWaterManagement, RangeCheck.NoSpecialBase));
            //ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epLakeWaterManagement, "Controls Lake Water Management: increased or decreased groundwater withdrawals.", "", "Scenario changes in lake later withdrawals", "", new string[4] { "Less", "None", "More", "Most" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAugmented, "Augmented Desal", "DESAL", rangeChecktype.rctCheckRange, 0, 200, WSmith.geti_Desalinization, WSmith.seti_Desalinization, RangeCheck.NoSpecialBase));
            //ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAugmented, "Adds a percent of desalinaiton: increased surface saline withdrawals.", "", "Scenario changes in lake later withdrawals", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 0, 100, 150, 200 }, new ModelParameterGroupClass[] { }));
            //
            _pm.AddParameter(new ModelParameterClass(eModelParam.epPolicyStartYear, "Policy Start Year", "PST", rangeChecktype.rctCheckRange, 2016, 2060, geti_PolicyStartYear, seti_PolicyStartYear, RangeCheck.NoSpecialBase));
               ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPolicyStartYear, "Year that the Policies are implemented", "yr", "Year", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //
            // Controls - External Forcings
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epPopGrowthAdjustment, "Population Growth Projected", "POPGR", rangeChecktype.rctCheckRange, 0, 150, WSmith.geti_PopGrowthRate, WSmith.seti_PopGrowthRate, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epPopGrowthAdjustment, "Adjustment in the Projected Population Growth Rate.", "%", "Population Growth", "", new string[4] { "Low", "Some", "Planned", "High" }, new int[4] { 60, 80, 100, 120 }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epClimateDrought, "Drought Impacts on Rivers/Lakes ", "CLIM", rangeChecktype.rctCheckRange, 0, 4, WSmith.geti_DroughtImpacts, WSmith.seti_DroughtImpacts, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epClimateDrought, "Alteration in Fresh Water Withdrawals as a result of drought on supplies.", "Scenario-driven", "Drought Reductions in Surface Water", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            ////
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epDroughtControl, "Drought Impacts Control- controls rate", "DC", rangeChecktype.rctCheckRange, 50, 150, WSmith.geti_DroughtControl, WSmith.seti_DroughtControl, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epDroughtControl, "Percent reduction in Surface flows due to drought", "%", "Percent (%)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAgricultureProduction, "Agriculture Net $", "ANP", WSmith.geti_AgricutureProduction));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAgricultureProduction, "Agriculture Net Annual Farm Income.", "M$", "Million Dollars ", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //_pm.AddParameter(new ModelParameterClass(eModelParam.epAgriculturalGrowth, "Agriculture Growth", "AGGR", rangeChecktype.rctCheckRange, 50, 150, WSmith.geti_AgGrowthRate, WSmith.seti_AgGrowthRate, RangeCheck.NoSpecialBase));
            //   ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epAgriculturalGrowth, "Agriculture Growth Rate Applied.", "%", "Percent of current growth", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));



            // -----------------------------
            // Initialize Other
               WestModel.startYear = _StartYear;
               WestModel.endYear = _EndYear;
               WestModel.currentYear = _StartYear;

            //WSmith.startYear = _StartYear;
            //WSmith.endYear = _EndYear;
            //WSmith.currentYear = _StartYear;
            // =============================
            //
            defaultSettings();
            //
            // 12.14.16 added
            WestModel.policyStartYear = geti_PolicyStartYear();

            //WSmith.policyStartYear = geti_PolicyStartYear();

            #region WestModelParameters
            //============================================================================================================================
            // WEST MODEL PARAMETERS
            // ===========================================================================================================================

            // POPULATION
            //WestModel.Population = new providerArrayProperty(_pm,eModelParam.epP_Population, WestModel.get_Population, eProviderAggregateMode.agSum);
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epP_Population, "Population Served", "MPOP_P", WestModel.Population ));
            //ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Population, "Unit Population People, in any given year- we use an estimate of slope to project out to 2065", "ppl", "Population (ppl)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));
            //// GPCD
            //WestModel.GPCD = new providerArrayProperty(_pm, eModelParam.epP_GPCD_urban, WestModel.get_Population, eProviderAggregateMode.agSum);
            //_pm.AddParameter(new ModelParameterClass(eModelParam.epP_GPCD_urban, "Urban GPCD", "UGPCD_P", WSmith.geti_gpcd));
            //ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GPCD_urban, "The GPCD (Gallons per Capita per Day) for delivered water for the Urban water sector.", "GPCDP", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // Population
            WestModel.Population = new providerArrayProperty(_pm, eModelParam.epP_Population, WestModel.geti_Pop, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Population, "Population Served", "PO_P", WestModel.Population));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Population, "State Population People in any given year- we use an estimate of slope to project out to 2065", "ppl", "State Population (ppl)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // GPCD_urban
            WestModel.GPCD_urban = new providerArrayProperty(_pm, eModelParam.epP_GPCD_urban, WestModel.geti_gpcd, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GPCD_urban, "Urban GPCD", "UGPC_P", WestModel.GPCD_urban));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GPCD_urban, "The GPCD (Gallons per Capita per Day) for delivered water for the Urban water sector.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // GPCD_ag
            WestModel.GPCD_ag = new providerArrayProperty(_pm, eModelParam.epP_GPCD_ag, WestModel.geti_gpcdAg, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GPCD_ag, "Agricultural GPCD", "AGPC_P", WestModel.GPCD_ag));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GPCD_ag, "The GPCD (Gallons per Capita per Day) for delivered water for Agricultural Uses.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // GPCD_other
            WestModel.GPCD_other = new providerArrayProperty(_pm, eModelParam.epP_GPCD_other, WestModel.geti_gpcdOther, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GPCD_other, "Other GPCD: Power and Industry", "OGPC_P", WestModel.GPCD_other));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GPCD_other, "The GPCD (Gallons per Capita per Day) for delivered water for Industrial Uses and Power Combined.", "GPCD", "Gallons per Capita per Day (GPCD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // SurfaceFresh
            WestModel.SurfaceFresh = new providerArrayProperty(_pm, eModelParam.epP_SurfaceFresh, WestModel.geti_SurfaceWaterFresh, WestModel.seti_SurfaceWaterFresh, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceFresh, "Surface Water (Fresh)", "SU_P", WestModel.SurfaceFresh));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SurfaceFresh, "Fresh Water Deliveries from Surface Sources; this is total fresh water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // SurfaceFreshNet
            WestModel.SurfaceFreshNet = new providerArrayProperty(_pm, eModelParam.epP_SurfaceFreshNet, WestModel.geti_SurfaceWaterFreshNet, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceFreshNet, "Surface Water (Fresh) Net", "SUR_P", WestModel.SurfaceFreshNet));

            // SurfaceSaline
            WestModel.SurfaceSaline = new providerArrayProperty(_pm, eModelParam.epP_SurfaceSaline, WestModel.geti_SurfaceWaterSaline, WestModel.seti_SurfaceWaterSaline, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceSaline, "Surface Water (Saline)", "SA_P", WestModel.SurfaceSaline));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SurfaceSaline, "Saline Water Deliveries from Surface Sources; this is total saline water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // SurfaceSalineNet
            WestModel.SurfaceSalineNet = new providerArrayProperty(_pm, eModelParam.epP_SurfaceSalineNet, WestModel.geti_SurfaceWaterSalineNet, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceSalineNet, "Surface Water (Saline) Net", "SAL_P", WestModel.SurfaceSalineNet));

            // Groundwater
            WestModel.Groundwater = new providerArrayProperty(_pm, eModelParam.epP_Groundwater, WestModel.geti_Groundwater, WestModel.seti_Groundwater, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Groundwater, "Groundwater (Fresh)", "G_P", WestModel.Groundwater));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Groundwater, "Fresh Water Deliveries from Pumped Groundwater; this is total water withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // GroundwaterNet
            WestModel.GroundwaterNet = new providerArrayProperty(_pm, eModelParam.epP_GroundwaterNet, WestModel.geti_GroundwaterNet, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GroundwaterNet, "Groundwater (Fresh) Net", "GW_P", WestModel.GroundwaterNet));

            // Effluent
            WestModel.Effluent = new providerArrayProperty(_pm, eModelParam.epP_Effluent, WestModel.geti_Effluent, WestModel.seti_Effluent, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Effluent, "Effluent (Reclaimed)", "RE_P", WestModel.Effluent));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Effluent, "Effluent (reclaimed) Water Deliveries from Waste Water Treatment Plants; total withdrawals.", "MGD", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // EffluentNet
            WestModel.EffluentNet = new providerArrayProperty(_pm, eModelParam.epP_EffluentNet, WestModel.geti_EffluentNet, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_EffluentNet, "Effluent (Reclaimed) Net", "REC_P", WestModel.EffluentNet));

            // SurfaceLake
            WestModel.SurfaceLake = new providerArrayProperty(_pm, eModelParam.epP_SurfaceLake, WestModel.geti_SurfaceLake, WestModel.seti_SurfaceLake, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceLake, "Surface Lake Water", "SUR_P", WestModel.SurfaceLake));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SurfaceLake, "Surface Lake Water", "mgd", "Million Gallons Per Day", "Surface Lake Water", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // SurfaceLakeNet
            WestModel.SurfaceLakeNet = new providerArrayProperty(_pm, eModelParam.epP_SurfaceLakeNet, WestModel.geti_SurfaceLakeNet, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceLakeNet, "Surface Lake Water Net", "SURL_P", WestModel.SurfaceLakeNet));

            // TotalSupplies
            WestModel.TotalSupplies = new providerArrayProperty(_pm, eModelParam.epP_TotalSupplies, WestModel.geti_TotalSupplies, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_TotalSupplies, "Total Supplies", "T_P", WestModel.TotalSupplies));

            // Urban
            WestModel.Urban = new providerArrayProperty(_pm, eModelParam.epP_Urban, WestModel.geti_Urban, WestModel.seti_Urban, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Urban, "Urban Demand", "U_P", WestModel.Urban));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Urban, "Urban Water Demand", "MGD ", "Million Gallons per Day", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // UrbanNet
            WestModel.UrbanNet = new providerArrayProperty(_pm, eModelParam.epP_UrbanNet, WestModel.geti_Urban_Net, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_UrbanNet, "Urban Demand (Net)", "UD_P", WestModel.UrbanNet));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_UrbanNet, "Urban (residential) Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // Agriculture
            WestModel.Agriculture = new providerArrayProperty(_pm, eModelParam.epP_Agriculture, WestModel.geti_Agriculture, WestModel.seti_Agriculture, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Agriculture, "Agriculture Demand", "A_P", WestModel.Agriculture));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Agriculture, "Agriculture Water Demand; total withdrawals.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // AgricultureNet
            WestModel.AgricultureNet = new providerArrayProperty(_pm, eModelParam.epP_AgricultureNet, WestModel.geti_Agriculture_Net, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_AgricultureNet, "Agriculture Demand (Net)", "AD_P", WestModel.AgricultureNet));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_AgricultureNet, "Agricultural Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // Industrial
            WestModel.Industrial = new providerArrayProperty(_pm, eModelParam.epP_Industrial, WestModel.geti_Industrial, WestModel.seti_Industrial, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Industrial, "Industrial Demand", "I_P", WestModel.Industrial));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Industrial, "Industrial Water Demand; total withdrawals. Water used for industries such as steel, chemical, paper, and petroleum refining. ", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // IndustrialNet
            WestModel.IndustrialNet = new providerArrayProperty(_pm, eModelParam.epP_IndustrialNet, WestModel.geti_Industrial_Net, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_IndustrialNet, "Industrial Demand (Net)", "ID_P", WestModel.IndustrialNet));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_IndustrialNet, "Industrial Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // Power
            WestModel.Power = new providerArrayProperty(_pm, eModelParam.epP_Power, WestModel.geti_PowerWater, WestModel.seti_PowerWater, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Power, "Power Demand", "P_P", WestModel.Power));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Power, "Water Use by Power: total withdrawals. Water used in the process of generating electricity with steam-driven turbine generators [Thermoelectric power, subcategories by cooling-system type (once-through, closed-loop/recirculation)].", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // PowerNet
            WestModel.PowerNet = new providerArrayProperty(_pm, eModelParam.epP_PowerNet, WestModel.geti_PowerWater_Net, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_PowerNet, "Power Demand (Net)", "PD_P", WestModel.PowerNet));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_PowerNet, "Power Net Water Balance; the difference between source withdrawals and demand.", "MGD ", "Million Gallons per Day (MGD)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // PowerEnergy
            WestModel.PowerEnergy = new providerArrayProperty(_pm, eModelParam.epP_PowerEnergy, WestModel.geti_PowerEnergy, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_PowerEnergy, "Power Produced", "P_P", WestModel.PowerEnergy));

            // NetDemandDifference
            WestModel.NetDemandDifference = new providerArrayProperty(_pm, eModelParam.epP_NetDemandDifference, WestModel.geti_NetDemandDifference, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_NetDemandDifference, "Net Demand Difference", "DDI_P", WestModel.NetDemandDifference));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_NetDemandDifference, "The ratio of net demand to total demand for all consumers; ", "% ", "Percent (%)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // UrbanWaterConservation
            WestModel.UrbanWaterConservation = new providerArrayProperty(_pm, eModelParam.epP_UrbanWaterConservation, WestModel.geti_UrbanConservation, WestModel.seti_UrbanConservation, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_UrbanWaterConservation, "Water Conservation (urban & rural)", "UCO_P", WestModel.UrbanWaterConservation));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_UrbanWaterConservation, "Urban Water Conservation: reduction in annual water use.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            // AgWaterConservation
            WestModel.AgWaterConservation = new providerArrayProperty(_pm, eModelParam.epP_AgWaterConservation, WestModel.geti_AgConservation, WestModel.seti_AgConservation, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_AgWaterConservation, "Ag Water Conservation", "ACO_P", WestModel.AgWaterConservation));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_AgWaterConservation, "Agricultural Water Conservation: reduction in annual water used by the Ag sector.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            // PowerWaterConservation
            WestModel.PowerWaterConservation = new providerArrayProperty(_pm, eModelParam.epP_PowerWaterConservation, WestModel.geti_PowerConservation, WestModel.seti_PowerConservation, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_PowerWaterConservation, "Power Water Conservation", "PCO_P", WestModel.PowerWaterConservation));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_PowerWaterConservation, "Power Water Conservation: reduction in annual water use for Thermoelectric power generation.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            // IndustrialWaterConservation
            WestModel.IndustrialWaterConservation = new providerArrayProperty(_pm, eModelParam.epP_IndustrialWaterConservation, WestModel.geti_IndustryConservation, WestModel.seti_IndustryConservation, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_IndustrialWaterConservation, "Industrial Water Conservation", "ICO_P", WestModel.IndustrialWaterConservation));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_IndustrialWaterConservation, "Industrial Water Conservation: reduction in annual water use for Industry.", "", "Percent reduction in water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 100, 80, 65, 50 }, new ModelParameterGroupClass[] { }));

            // SurfaceWaterManagement
            WestModel.SurfaceWaterManagement = new providerArrayProperty(_pm, eModelParam.epP_SurfaceWaterManagement, WestModel.geti_SurfaceWaterControl, WestModel.seti_SurfaceWaterControl, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SurfaceWaterManagement, "Use More Surface Water", "SW_P", WestModel.SurfaceWaterManagement));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SurfaceWaterManagement, "Controls Scenario Chosen for alteration in surface water supply: increased surface water withdrawals.", "", "Alteration in Available Surface Water", "", new string[4] { "Less", "None", "Med", "High" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));

            // GroundwaterManagement
            WestModel.GroundwaterManagement = new providerArrayProperty(_pm, eModelParam.epP_GroundwaterManagement, WestModel.geti_GroundwaterControl, WestModel.seti_GroundwaterControl, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GroundwaterManagement, "Change Groundwater Use", "GW_P", WestModel.GroundwaterManagement));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GroundwaterManagement, "Controls Scenario Chosen for alteration in groundwater supplies: increased or decreased groundwater withdrawals.", "", "Alteration in Groundwater Used", "", new string[4] { "Less", "None", "More", "Most" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));

            // ReclainedWaterUse
            WestModel.ReclainedWaterUse = new providerArrayProperty(_pm, eModelParam.epP_ReclainedWaterUse, WestModel.geti_ReclaimedWaterManagement, WestModel.seti_ReclaimedWaterManagement, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_ReclainedWaterUse, "Use Reclaimed Water", "REC_P", WestModel.ReclainedWaterUse));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_ReclainedWaterUse, "Alteration in reclaimed (effluent) supplies: increased effluent withdrawals.", "", "% of indoor water use", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 0, 33, 66, 100 }, new ModelParameterGroupClass[] { }));

            // LakeWaterManagement
            WestModel.LakeWaterManagement = new providerArrayProperty(_pm, eModelParam.epP_LakeWaterManagement, WestModel.geti_LakeWaterManagement, WestModel.seti_LakeWaterManagement, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_LakeWaterManagement, "Increase Lake Water use", "LW_P", WestModel.LakeWaterManagement));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_LakeWaterManagement, "Controls Lake Water Management: increased or decreased groundwater withdrawals.", "", "Scenario changes in lake later withdrawals", "", new string[4] { "Less", "None", "More", "Most" }, new int[4] { 80, 100, 120, 140 }, new ModelParameterGroupClass[] { }));

            // Augmented
            WestModel.Augmented = new providerArrayProperty(_pm, eModelParam.epP_Augmented, WestModel.geti_Desalinization, WestModel.seti_Desalinization, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_Augmented, "Augmented Desal", "DESA_P", WestModel.Augmented));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_Augmented, "Adds a percent of desalinaiton: increased surface saline withdrawals.", "", "Scenario changes in lake later withdrawals", "", new string[4] { "None", "Low", "Med", "High" }, new int[4] { 0, 100, 150, 200 }, new ModelParameterGroupClass[] { }));

            // PopGrowthAdjustment
            WestModel.PopGrowthAdjustment = new providerArrayProperty(_pm, eModelParam.epP_PopGrowthAdjustment, WestModel.geti_PopGrowthRate, WestModel.seti_PopGrowthRate, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_PopGrowthAdjustment, "Population Growth Projected", "POPG_P", WestModel.PopGrowthAdjustment));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_PopGrowthAdjustment, "Adjustment in the Projected Population Growth Rate.", "%", "Population Growth", "", new string[4] { "Low", "Some", "Planned", "High" }, new int[4] { 60, 80, 100, 120 }, new ModelParameterGroupClass[] { }));

            // ClimateDrought
            WestModel.ClimateDrought = new providerArrayProperty(_pm, eModelParam.epP_ClimateDrought, WestModel.geti_DroughtImpacts, WestModel.seti_DroughtImpacts, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_ClimateDrought, "Drought Impacts on Rivers/Lakes ", "CLI_P", WestModel.ClimateDrought));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_ClimateDrought, "Alteration in Fresh Water Withdrawals as a result of drought on supplies.", "Scenario-driven", "Drought Reductions in Surface Water", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // DroughtControl
            WestModel.DroughtControl = new providerArrayProperty(_pm, eModelParam.epP_DroughtControl, WestModel.geti_DroughtControl, WestModel.seti_DroughtControl, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_DroughtControl, "Drought Impacts Control- controls rate", "D_P", WestModel.DroughtControl));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_DroughtControl, "Percent reduction in Surface flows due to drought", "%", "Percent (%)", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // AgricultureProduction
            WestModel.AgricultureProduction = new providerArrayProperty(_pm, eModelParam.epP_AgricultureProduction, WestModel.geti_AgricutureProduction, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_AgricultureProduction, "Agriculture Net $", "AN_P", WestModel.AgricultureProduction));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_AgricultureProduction, "Agriculture Net Annual Farm Income.", "M$", "Million Dollars ", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // AgriculturalGrowth
            WestModel.AgriculturalGrowth = new providerArrayProperty(_pm, eModelParam.epP_AgriculturalGrowth, WestModel.geti_AgGrowthRate, WestModel.seti_AgGrowthRate, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_AgriculturalGrowth, "Agriculture Growth", "AGG_P", WestModel.AgriculturalGrowth));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_AgriculturalGrowth, "Agriculture Growth Rate Applied.", "%", "Percent of current growth", "", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            //==========================================
            // FLUXES
            // =======================================
            // _SUR_UD
            WestModel._SUR_UD = new providerArrayProperty(_pm, eModelParam.epP_SUR_UD, WestModel.geti_SUR_UD, WestModel.seti_SUR_UD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SUR_UD, "SUR to UTOT Allocation", "SUR_U_P", WestModel._SUR_UD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SUR_UD, "SUR Water Supply allocated to UTOT water consumption", "MGD", "Million Gallons Per Day", "SUR to UTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SUR_AD
            WestModel._SUR_AD = new providerArrayProperty(_pm, eModelParam.epP_SUR_AD, WestModel.geti_SUR_AD, WestModel.seti_SUR_AD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SUR_AD, "SUR to ATOT Allocation", "SUR_A_P", WestModel._SUR_AD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SUR_AD, "SUR Water Supply allocated to ATOT water consumption", "MGD", "Million Gallons Per Day", "SUR to ATOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SUR_ID
            WestModel._SUR_ID = new providerArrayProperty(_pm, eModelParam.epP_SUR_ID, WestModel.geti_SUR_ID, WestModel.seti_SUR_ID, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SUR_ID, "SUR to ITOT Allocation", "SUR_I_P", WestModel._SUR_ID));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SUR_ID, "SUR Water Supply allocated to ITOT water consumption", "MGD", "Million Gallons Per Day", "SUR to ITOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SUR_PD
            WestModel._SUR_PD = new providerArrayProperty(_pm, eModelParam.epP_SUR_PD, WestModel.geti_SUR_PD, WestModel.seti_SUR_PD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SUR_PD, "SUR to PTOT Allocation", "SUR_P_P", WestModel._SUR_PD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SUR_PD, "SUR Water Supply allocated to PTOT water consumption", "MGD", "Million Gallons Per Day", "SUR to PTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SURL_UD
            WestModel._SURL_UD = new providerArrayProperty(_pm, eModelParam.epP_SURL_UD, WestModel.geti_SURL_UD, WestModel.seti_SURL_UD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SURL_UD, "SURL to UTOT Allocation", "SURL_U_P", WestModel._SURL_UD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SURL_UD, "SURL Water Supply allocated to UTOT water consumption", "MGD", "Million Gallons Per Day", "SURL to UTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SURL_AD
            WestModel._SURL_AD = new providerArrayProperty(_pm, eModelParam.epP_SURL_AD, WestModel.geti_SURL_AD, WestModel.seti_SURL_AD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SURL_AD, "SURL to ATOT Allocation", "SURL_A_P", WestModel._SURL_AD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SURL_AD, "SURL Water Supply allocated to ATOT water consumption", "MGD", "Million Gallons Per Day", "SURL to ATOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SURL_ID
            WestModel._SURL_ID = new providerArrayProperty(_pm, eModelParam.epP_SURL_ID, WestModel.geti_SURL_ID, WestModel.seti_SURL_ID, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SURL_ID, "SURL to ITOT Allocation", "SURL_I_P", WestModel._SURL_ID));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SURL_ID, "SURL Water Supply allocated to ITOT water consumption", "MGD", "Million Gallons Per Day", "SURL to ITOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SURL_PD
            WestModel._SURL_PD = new providerArrayProperty(_pm, eModelParam.epP_SURL_PD, WestModel.geti_SURL_PD, WestModel.seti_SURL_PD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SURL_PD, "SURL to PTOT Allocation", "SURL_P_P", WestModel._SURL_PD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SURL_PD, "SURL Water Supply allocated to PTOT water consumption", "MGD", "Million Gallons Per Day", "SURL to PTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _GW_UD
            WestModel._GW_UD = new providerArrayProperty(_pm, eModelParam.epP_GW_UD, WestModel.geti_GW_UD, WestModel.seti_GW_UD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GW_UD, "GW to UTOT Allocation", "GW_U_P", WestModel._GW_UD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GW_UD, "GW Water Supply allocated to UTOT water consumption", "MGD", "Million Gallons Per Day", "GW to UTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _GW_AD
            WestModel._GW_AD = new providerArrayProperty(_pm, eModelParam.epP_GW_AD, WestModel.geti_GW_AD, WestModel.seti_GW_AD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GW_AD, "GW to ATOT Allocation", "GW_A_P", WestModel._GW_AD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GW_AD, "GW Water Supply allocated to ATOT water consumption", "MGD", "Million Gallons Per Day", "GW to ATOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _GW_ID
            WestModel._GW_ID = new providerArrayProperty(_pm, eModelParam.epP_GW_ID, WestModel.geti_GW_ID, WestModel.seti_GW_ID, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GW_ID, "GW to ITOT Allocation", "GW_I_P", WestModel._GW_ID));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GW_ID, "GW Water Supply allocated to ITOT water consumption", "MGD", "Million Gallons Per Day", "GW to ITOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _GW_PD
            WestModel._GW_PD = new providerArrayProperty(_pm, eModelParam.epP_GW_PD, WestModel.geti_GW_PD, WestModel.seti_GW_PD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_GW_PD, "GW to PTOT Allocation", "GW_P_P", WestModel._GW_PD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_GW_PD, "GW Water Supply allocated to PTOT water consumption", "MGD", "Million Gallons Per Day", "GW to PTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _REC_UD
            WestModel._REC_UD = new providerArrayProperty(_pm, eModelParam.epP_REC_UD, WestModel.geti_REC_UD, WestModel.seti_REC_UD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_REC_UD, "REC to UTOT Allocation", "REC_U_P", WestModel._REC_UD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_REC_UD, "REC Water Supply allocated to UTOT water consumption", "MGD", "Million Gallons Per Day", "REC to UTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _REC_AD
            WestModel._REC_AD = new providerArrayProperty(_pm, eModelParam.epP_REC_AD, WestModel.geti_REC_AD, WestModel.seti_REC_AD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_REC_AD, "REC to ATOT Allocation", "REC_A_P", WestModel._REC_AD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_REC_AD, "REC Water Supply allocated to ATOT water consumption", "MGD", "Million Gallons Per Day", "REC to ATOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _REC_ID
            WestModel._REC_ID = new providerArrayProperty(_pm, eModelParam.epP_REC_ID, WestModel.geti_REC_ID, WestModel.seti_REC_ID, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_REC_ID, "REC to ITOT Allocation", "REC_I_P", WestModel._REC_ID));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_REC_ID, "REC Water Supply allocated to ITOT water consumption", "MGD", "Million Gallons Per Day", "REC to ITOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _REC_PD
            WestModel._REC_PD = new providerArrayProperty(_pm, eModelParam.epP_REC_PD, WestModel.geti_REC_PD, WestModel.seti_REC_PD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_REC_PD, "REC to PTOT Allocation", "REC_P_P", WestModel._REC_PD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_REC_PD, "REC Water Supply allocated to PTOT water consumption", "MGD", "Million Gallons Per Day", "REC to PTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SAL_UD
            WestModel._SAL_UD = new providerArrayProperty(_pm, eModelParam.epP_SAL_UD, WestModel.geti_SAL_UD, WestModel.seti_SAL_UD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SAL_UD, "SAL to UTOT Allocation", "SAL_U_P", WestModel._SAL_UD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SAL_UD, "SAL Water Supply allocated to UTOT water consumption", "MGD", "Million Gallons Per Day", "SAL to UTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SAL_AD
            WestModel._SAL_AD = new providerArrayProperty(_pm, eModelParam.epP_SAL_AD, WestModel.geti_SAL_AD, WestModel.seti_SAL_AD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SAL_AD, "SAL to ATOT Allocation", "SAL_A_P", WestModel._SAL_AD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SAL_AD, "SAL Water Supply allocated to ATOT water consumption", "MGD", "Million Gallons Per Day", "SAL to ATOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SAL_ID
            WestModel._SAL_ID = new providerArrayProperty(_pm, eModelParam.epP_SAL_ID, WestModel.geti_SAL_ID, WestModel.seti_SAL_ID, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SAL_ID, "SAL to ITOT Allocation", "SAL_I_P", WestModel._SAL_ID));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SAL_ID, "SAL Water Supply allocated to ITOT water consumption", "MGD", "Million Gallons Per Day", "SAL to ITOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));

            // _SAL_PD
            WestModel._SAL_PD = new providerArrayProperty(_pm, eModelParam.epP_SAL_PD, WestModel.geti_SAL_PD, WestModel.seti_SAL_PD, eProviderAggregateMode.agSum);
            _pm.AddParameter(new ModelParameterClass(eModelParam.epP_SAL_PD, "SAL to PTOT Allocation", "SAL_P_P", WestModel._SAL_PD));
            ExtendDoc.Add(new WaterSimDescripItem(eModelParam.epP_SAL_PD, "SAL Water Supply allocated to PTOT water consumption", "MGD", "Million Gallons Per Day", "SAL to PTOT", new string[] { }, new int[] { }, new ModelParameterGroupClass[] { }));



            #endregion WestModelParameters
        }
        void defaultSettings()
        {
            // default settings
            // ------------------------------------
            startDrought = 2016;
            endDrought = 2050;
            seti_PolicyStartYear(2016);
            // ====================================
        }
        void resetManager()
        {
            //int temp=100;
            //seti_agTargetEfficiency(temp);
        }
        // =====================================================================================================================
        //
        /// <summary>
        /// Policy Start Year; starts the year in which any policy 
        /// starts; valid are 2016 to 2060 (at present)
        /// </summary>
        int _policyStartYear = 2015;
        public int geti_PolicyStartYear()
        {
            return _policyStartYear;
        }
        public void seti_PolicyStartYear(int value)
        {
            //_policyStartYear = value;
            startSGWM = value;
        }
        // ==============================
//        int _waterManagementStart = 2016;
        public int startSGWM
        {
            get { return _policyStartYear; }
            set { _policyStartYear = value; }
        }
        int _waterManagementEnd = 2050;
        public int endSGWM
        {
            get { return _waterManagementEnd; }
            set { _waterManagementEnd = value; }
        }
        // ============================================
        int _droughtStart = 0;
        public int startDrought
        {
            get { return _droughtStart; }
            set 
            { 
                _droughtStart = value;
              //WSmith.startDroughtYear = value;
              WestModel.StartDroughtYear = value;
                
            }
        }
        int _droughtEnd = 0;
        public int endDrought
        {
            get { return _droughtEnd; }
            set { _droughtEnd = value; }
        }
        // 02.25.16 DAS
        //int geti_Pop()
        //{
        //    // 07.19.16 DAS
        //    // stop from throwing an index error
        //    int pop = 0;
        //    int stopyear = WSmith.endYear;
        //    if (Sim_CurrentYear <= stopyear)
        //    {
        //        int year = Sim_CurrentYear;
        //        //pop = WSmith.Get_PopYear(year);
        //        pop = WSmith.geti_NewPopulation();// .Get_PopYear(year);
        //    }
        //    return pop;
        //}

        #region WaterSmithParamters
        // ---------------------------------------------------------------------------
        // ==============================================================================================================
     
    
        //
        //**************************************************************
        //
        // WATERSMITH PARAMETERS
        //
        //**************************************************************

      
        #endregion
        //
        public virtual bool Model()
        {
            bool success = false;
            
            return success;
        }
        //
        //  This code indexes the WaterSim America Model
        public const int FNumberOfStates = 11;
        public static string[] FStateNames = new string[FNumberOfStates] { "Florida", "Idaho", "Illinois", "Minnesota", "Wyoming", "Arizona", "Colorado", "Nevada", "California", "Utah", "NewMexico" };

        public int FStateIndex = 0;


 
    }
    
    // new stuff

}
