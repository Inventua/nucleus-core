using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Sharp.MICASAgent.Abstractions.Models;

namespace MICAS.Mobile.Service.Models
{
  public class Device : Sharp.MICASAgent.Abstractions.Models.IDevice
  {
    [Flags]
    enum DataFlags : int
    {
      Devices = 1,
      Counters = 2,
      TonerHistory = 4,
      TonerLevels = 8,
      MaintenanceTasks = 16,
      Parts = 32,
      Forecast = 64,
      SupplyLevels = 128
    }

    public Device()
    {
    }

    [Newtonsoft.Json.JsonProperty("manufacturer")]
    public string Manufacturer { get; set; }

    [Newtonsoft.Json.JsonProperty("product")]
    public string Product { get; set; }

    [Newtonsoft.Json.JsonProperty("model")]
    public string ModelName { get; set; }
    
    [Newtonsoft.Json.JsonProperty("serialNumber")]
    public string SerialNumber { get; set; }

    [Newtonsoft.Json.JsonProperty("agentId")]
    public Guid? AgentId { get; set; }

    [Newtonsoft.Json.JsonProperty("machineId")]
    public string MachineID { get; set; }

    [Newtonsoft.Json.JsonProperty("machineCode")]
    public string MachineCode { get; set; }

    [Newtonsoft.Json.JsonProperty("ipAddress")]
    public string IPAddress { get; set; }

    [Newtonsoft.Json.JsonProperty("name")]
    public string Name { get; set; }

    [Newtonsoft.Json.JsonProperty("location")]
    public string Location { get; set; }

		[Newtonsoft.Json.JsonProperty("id")]
		public Guid ID { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public Guid PrinterID { get; set; }

    [Newtonsoft.Json.JsonProperty("counterTotals")]
    public TotalList Totals { get; set; } = new TotalList();

    [Newtonsoft.Json.JsonProperty("toners")]
    public List<TonerLevel> TonerLevels { get; set; } = new List<TonerLevel>();

    [Newtonsoft.Json.JsonIgnore]
    internal List<TonerLevelHistoryItem> TonerLevelHistory { get; set; } = new List<TonerLevelHistoryItem>();

    [Newtonsoft.Json.JsonProperty("maintenanceTasks")]
    public List<MaintenanceTask> MaintenanceTasks { get; set; } = new List<MaintenanceTask>();

    [Newtonsoft.Json.JsonProperty("supplyLevels")]
    public List<SupplyLevel> SupplyLevels { get; set; } = new List<SupplyLevel>();

    [Newtonsoft.Json.JsonProperty("events")]
    public List<EventItem> Events { get; set; } = new List<EventItem>();

    [Newtonsoft.Json.JsonProperty("alerts")]
    public List<Alert> Alerts { get; set; } = new List<Alert>();

    [Newtonsoft.Json.JsonProperty("firmware")]
    public Firmware Firmware { get; set; }

    [Newtonsoft.Json.JsonProperty("telemetry")]
    public Telemetry Telemetry { get; set; }

    [Newtonsoft.Json.JsonProperty("isDecommissioned")]
    public Boolean IsDecommissioned { get; set; }
  }

  [Newtonsoft.Json.JsonObject("event")]
  public class EventItem
  {
    public enum JobModes : int
    {
      [System.ComponentModel.Description("n/a")]
      NULL = -1,
      [System.ComponentModel.Description("Shading")]
      SHADING = 0x0,
      [System.ComponentModel.Description("Process Control")]
      PROCESS_CONTROL = 0x1,
      [System.ComponentModel.Description("Test Mode (Sim)")]
      TEST_MODE_SIM = 0x2,
      [System.ComponentModel.Description("Interrupt Copy")]
      INTERRUPT_COPY = 0x3,
      [System.ComponentModel.Description("Copy")]
      COPY = 0x4,
      [System.ComponentModel.Description("Scan to Fax")]
      SCAN_TO_FAX = 0x5,
      [System.ComponentModel.Description("AXIS")]
      AXIS = 0x6,
      [System.ComponentModel.Description("Prints received document")]
      PRINTS_RECEIVED_DOCUMENT = 0x7,
      [System.ComponentModel.Description("Printer")]
      PRINTER = 0x8,
      [System.ComponentModel.Description("Activity Report")]
      ACTIVITY_REPORT = 0x9,
      [System.ComponentModel.Description("Zaurus Print")]
      ZAURUS_PRINT = 0xA,
      [System.ComponentModel.Description("Self/Test Print")]
      SELF_TEST_PRINT = 0xB,
      [System.ComponentModel.Description("Original Counter")]
      ORIGINAL_COUNTER = 0xC,
      [System.ComponentModel.Description("Remote Maintenance")]
      REMOTE_MAINTENANCE = 0xD,
      [System.ComponentModel.Description("Simulation 52-1")]
      SIMULATION_52_1 = 0xE,
      [System.ComponentModel.Description("Tandem (Sub-Machine)")]
      TANDEM_SUB_MACHINE = 0xF,
      [System.ComponentModel.Description("Confidential Print")]
      CONFIDENTIAL_PRINT = 0x10,
      [System.ComponentModel.Description("Network Scanner")]
      NETWORK_SCANNER = 0x11,
      [System.ComponentModel.Description("Proof Print")]
      PROOF_PRINT = 0x12
    }

    public enum EventTypes : int
    {
      [System.ComponentModel.Description("Document Feeder Jam")]
      DFJam = 0,
      [System.ComponentModel.Description("Main Body or Original Jam")]
      MBJam = 1,
      [System.ComponentModel.Description("Trouble")]
      Trouble = 2,
      [System.ComponentModel.Description("Copier Jam")]
      CopierJam = 3
    }

    public enum PaperSizes : int
    {
      [System.ComponentModel.Description("n/a")]
      NULL = -1,
      [System.ComponentModel.Description("NONE")]
      PAPER_CODE_NONE = 0x0,
      [System.ComponentModel.Description("W_LEGAL")]
      PAPER_CODE_W_LEGAL = 0x2,
      [System.ComponentModel.Description("W_LEGAL_R")]
      PAPER_CODE_W_LEGAL_R = 0x3,
      [System.ComponentModel.Description("LEDGER")]
      PAPER_CODE_LEDGER = 0x4,
      [System.ComponentModel.Description("LEDGER_R")]
      PAPER_CODE_LEDGER_R = 0x5,
      [System.ComponentModel.Description("LEGAL")]
      PAPER_CODE_LEGAL = 0x6,
      [System.ComponentModel.Description("LEGAL_R")]
      PAPER_CODE_LEGAL_R = 0x7,
      [System.ComponentModel.Description("FOOLSCAP")]
      PAPER_CODE_FOOLSCAP = 0x8,
      [System.ComponentModel.Description("FOOLSCAP_R")]
      PAPER_CODE_FOOLSCAP_R = 0x9,
      [System.ComponentModel.Description("LETTER")]
      PAPER_CODE_LETTER = 0xA,
      [System.ComponentModel.Description("LETTER_R")]
      PAPER_CODE_LETTER_R = 0xB,
      [System.ComponentModel.Description("INVOICE")]
      PAPER_CODE_INVOICE = 0xC,
      [System.ComponentModel.Description("INVOICE_R")]
      PAPER_CODE_INVOICE_R = 0xD,
      [System.ComponentModel.Description("EXECUTIVE")]
      PAPER_CODE_EXECUTIVE = 0xE,
      [System.ComponentModel.Description("EXECUTIVE_R")]
      PAPER_CODE_EXECUTIVE_R = 0xF,
      [System.ComponentModel.Description("A3W")]
      PAPER_CODE_A3W = 0x10,
      [System.ComponentModel.Description("A3W_R")]
      PAPER_CODE_A3W_R = 0x11,
      [System.ComponentModel.Description("22_17")]
      PAPER_CODE_22_17 = 0x12,
      [System.ComponentModel.Description("22_17_R")]
      PAPER_CODE_22_17_R = 0x13,
      [System.ComponentModel.Description("22_34")]
      PAPER_CODE_22_34 = 0x14,
      [System.ComponentModel.Description("22_34_R")]
      PAPER_CODE_22_34_R = 0x15,
      [System.ComponentModel.Description("34_44")]
      PAPER_CODE_34_44 = 0x16,
      [System.ComponentModel.Description("34_44_R")]
      PAPER_CODE_34_44_R = 0x17,
      [System.ComponentModel.Description("44_68")]
      PAPER_CODE_44_68 = 0x18,
      [System.ComponentModel.Description("44_68_R")]
      PAPER_CODE_44_68_R = 0x19,
      [System.ComponentModel.Description("9_12")]
      PAPER_CODE_9_12 = 0x1A,
      [System.ComponentModel.Description("9_12_R")]
      PAPER_CODE_9_12_R = 0x1B,
      [System.ComponentModel.Description("13_19")]
      PAPER_CODE_13_19 = 0x1,
      [System.ComponentModel.Description("13_19_R")]
      PAPER_CODE_13_19_R = 0x1D,
      [System.ComponentModel.Description("M_LEGAL")]
      PAPER_CODE_M_LEGAL = 0x1E,
      [System.ComponentModel.Description("M_LEGAL_R")]
      PAPER_CODE_M_LEGAL_R = 0x1F,
      [System.ComponentModel.Description("UNKNOWN_33")]
      PAPER_CODE_UNKNOWN_33 = 0x21,
      [System.ComponentModel.Description("UNKNOWN_62")]
      PAPER_CODE_UNKNOWN_62 = 0x3E,
      [System.ComponentModel.Description("EXTRA")]
      PAPER_CODE_EXTRA = 0x3F,
      [System.ComponentModel.Description("UNCERTAIN")]
      PAPER_CODE_UNCERTAIN = 0xEF,
      [System.ComponentModel.Description("EXTRA_LARGE")]
      PAPER_CODE_EXTRA_LARGE = 0xE,
      [System.ComponentModel.Description("EXTRA_SMALL")]
      PAPER_CODE_EXTRA_SMALL = 0xED,
      [System.ComponentModel.Description("A1")]
      PAPER_CODE_A1 = 0x40,
      [System.ComponentModel.Description("A1_R")]
      PAPER_CODE_A1_R = 0x41,
      [System.ComponentModel.Description("A2")]
      PAPER_CODE_A2 = 0x42,
      [System.ComponentModel.Description("A2_R")]
      PAPER_CODE_A2_R = 0x43,
      [System.ComponentModel.Description("A3")]
      PAPER_CODE_A3 = 0x44,
      [System.ComponentModel.Description("A3_R")]
      PAPER_CODE_A3_R = 0x45,
      [System.ComponentModel.Description("A4")]
      PAPER_CODE_A4 = 0x46,
      [System.ComponentModel.Description("A4_R")]
      PAPER_CODE_A4_R = 0x47,
      [System.ComponentModel.Description("A5")]
      PAPER_CODE_A5 = 0x48,
      [System.ComponentModel.Description("A5_R")]
      PAPER_CODE_A5_R = 0x49,
      [System.ComponentModel.Description("A6")]
      PAPER_CODE_A6 = 0x4A,
      [System.ComponentModel.Description("A6_R")]
      PAPER_CODE_A6_R = 0x4B,
      [System.ComponentModel.Description("B3")]
      PAPER_CODE_B3 = 0x4,
      [System.ComponentModel.Description("B3_R")]
      PAPER_CODE_B3_R = 0x4D,
      [System.ComponentModel.Description("B4")]
      PAPER_CODE_B4 = 0x4E,
      [System.ComponentModel.Description("B4_R")]
      PAPER_CODE_B4_R = 0x4F,
      [System.ComponentModel.Description("B5")]
      PAPER_CODE_B5 = 0x50,
      [System.ComponentModel.Description("B5_R")]
      PAPER_CODE_B5_R = 0x51,
      [System.ComponentModel.Description("B6")]
      PAPER_CODE_B6 = 0x52,
      [System.ComponentModel.Description("B6_R")]
      PAPER_CODE_B6_R = 0x53,
      [System.ComponentModel.Description("A02")]
      PAPER_CODE_A02 = 0x54,
      [System.ComponentModel.Description("A02_R")]
      PAPER_CODE_A02_R = 0x55,
      [System.ComponentModel.Description("A0")]
      PAPER_CODE_A0 = 0x56,
      [System.ComponentModel.Description("A0_R")]
      PAPER_CODE_A0_R = 0x57,
      [System.ComponentModel.Description("B0")]
      PAPER_CODE_B0 = 0x58,
      [System.ComponentModel.Description("B0_R")]
      PAPER_CODE_B0_R = 0x59,
      [System.ComponentModel.Description("B1")]
      PAPER_CODE_B1 = 0x5A,
      [System.ComponentModel.Description("B1_R")]
      PAPER_CODE_B1_R = 0x5B,
      [System.ComponentModel.Description("B2")]
      PAPER_CODE_B2 = 0x5,
      [System.ComponentModel.Description("B2_R")]
      PAPER_CODE_B2_R = 0x5D,
      [System.ComponentModel.Description("K8")]
      PAPER_CODE_K8 = 0x60,
      [System.ComponentModel.Description("K8_R")]
      PAPER_CODE_K8_R = 0x61,
      [System.ComponentModel.Description("K16")]
      PAPER_CODE_K16 = 0x62,
      [System.ComponentModel.Description("K16_R")]
      PAPER_CODE_K16_R = 0x63,
      [System.ComponentModel.Description("32K")]
      PAPER_CODE_32K = 0x64,
      [System.ComponentModel.Description("32KR")]
      PAPER_CODE_32KR = 0x65,
      [System.ComponentModel.Description("SRA3")]
      PAPER_CODE_SRA3 = 0x66,
      [System.ComponentModel.Description("SRA3_R")]
      PAPER_CODE_SRA3_R = 0x67,
      [System.ComponentModel.Description("SRA4")]
      PAPER_CODE_SRA4 = 0x68,
      [System.ComponentModel.Description("SRA4_R")]
      PAPER_CODE_SRA4_R = 0x69,
      [System.ComponentModel.Description("KIKU4")]
      PAPER_CODE_KIKU4 = 0x6A,
      [System.ComponentModel.Description("KIKU4_R")]
      PAPER_CODE_KIKU4_R = 0x6B,
      [System.ComponentModel.Description("KIKU8")]
      PAPER_CODE_KIKU8 = 0x6,
      [System.ComponentModel.Description("KIKU8_R")]
      PAPER_CODE_KIKU8_R = 0x6D,
      [System.ComponentModel.Description("A_CUT4")]
      PAPER_CODE_A_CUT4 = 0x6E,
      [System.ComponentModel.Description("A_CUT4_R")]
      PAPER_CODE_A_CUT4_R = 0x6F,
      [System.ComponentModel.Description("A_CUT8")]
      PAPER_CODE_A_CUT8 = 0x70,
      [System.ComponentModel.Description("A_CUT8_R")]
      PAPER_CODE_A_CUT8_R = 0x71,
      [System.ComponentModel.Description("WPOSTCARD")]
      PAPER_CODE_WPOSTCARD = 0x82,
      [System.ComponentModel.Description("WPOSTCARD_R")]
      PAPER_CODE_WPOSTCARD_R = 0x83,
      [System.ComponentModel.Description("POSTCARD")]
      PAPER_CODE_POSTCARD = 0x84,
      [System.ComponentModel.Description("POSTCARD_R")]
      PAPER_CODE_POSTCARD_R = 0x85,
      [System.ComponentModel.Description("RECTANGLE2_R")]
      PAPER_CODE_RECTANGLE2_R = 0x87,
      [System.ComponentModel.Description("RECTANGLE3_R")]
      PAPER_CODE_RECTANGLE3_R = 0x89,
      [System.ComponentModel.Description("RECTANGLE4_R")]
      PAPER_CODE_RECTANGLE4_R = 0x8B,
      [System.ComponentModel.Description("RECTANGLE5_R")]
      PAPER_CODE_RECTANGLE5_R = 0x8D,
      [System.ComponentModel.Description("SQURE2_R")]
      PAPER_CODE_SQURE2_R = 0x8F,
      [System.ComponentModel.Description("SQURE3_R")]
      PAPER_CODE_SQURE3_R = 0x91,
      [System.ComponentModel.Description("SQURE4_R")]
      PAPER_CODE_SQURE4_R = 0x93,
      [System.ComponentModel.Description("SQURE5_R")]
      PAPER_CODE_SQURE5_R = 0x95,
      [System.ComponentModel.Description("SQURE6_R")]
      PAPER_CODE_SQURE6_R = 0x97,
      [System.ComponentModel.Description("SQURE7_R")]
      PAPER_CODE_SQURE7_R = 0x99,
      [System.ComponentModel.Description("SQURE8_R")]
      PAPER_CODE_SQURE8_R = 0x9B,
      [System.ComponentModel.Description("FOREGIN1_R")]
      PAPER_CODE_FOREGIN1_R = 0x9D,
      [System.ComponentModel.Description("FOREGIN2_R")]
      PAPER_CODE_FOREGIN2_R = 0x9F,
      [System.ComponentModel.Description("FOREGIN3_R")]
      PAPER_CODE_FOREGIN3_R = 0xA1,
      [System.ComponentModel.Description("FOREGIN4_R")]
      PAPER_CODE_FOREGIN4_R = 0xA3,
      [System.ComponentModel.Description("FOREGIN5_R")]
      PAPER_CODE_FOREGIN5_R = 0xA5,
      [System.ComponentModel.Description("FOREGIN6_R")]
      PAPER_CODE_FOREGIN6_R = 0xA7,
      [System.ComponentModel.Description("FOREGIN7_R")]
      PAPER_CODE_FOREGIN7_R = 0xA9,
      [System.ComponentModel.Description("AB_E")]
      PAPER_CODE_AB_E = 0xAA,
      [System.ComponentModel.Description("AB_L")]
      PAPER_CODE_AB_L = 0xAB,
      [System.ComponentModel.Description("AB_PANORAMA")]
      PAPER_CODE_AB_PANORAMA = 0xA,
      [System.ComponentModel.Description("AB_CARD_LARGE")]
      PAPER_CODE_AB_CARD_LARGE = 0xAD,
      [System.ComponentModel.Description("AB_PROOFPHOTO")]
      PAPER_CODE_AB_PROOFPHOTO = 0xAE,
      [System.ComponentModel.Description("AB_CARD_SMALL")]
      PAPER_CODE_AB_CARD_SMALL = 0xAF,
      [System.ComponentModel.Description("A3_WIDTH")]
      PAPER_CODE_A3_WIDTH = 0xB0,
      [System.ComponentModel.Description("B4_WIDTH")]
      PAPER_CODE_B4_WIDTH = 0xB1,
      [System.ComponentModel.Description("A4_WIDTH")]
      PAPER_CODE_A4_WIDTH = 0xB2,
      [System.ComponentModel.Description("A3_WIDTH_LONG")]
      PAPER_CODE_A3_WIDTH_LONG = 0xB3,
      [System.ComponentModel.Description("B4_WIDTH_LONG")]
      PAPER_CODE_B4_WIDTH_LONG = 0xB4,
      [System.ComponentModel.Description("A4_WIDTH_LONG")]
      PAPER_CODE_A4_WIDTH_LONG = 0xB5,
      [System.ComponentModel.Description("CUSTOM")]
      PAPER_CODE_CUSTOM = 0xBF,
      [System.ComponentModel.Description("CUSTOM_LARGE")]
      PAPER_CODE_CUSTOM_LARGE = 0xB,
      [System.ComponentModel.Description("CUSTOM_SMALL")]
      PAPER_CODE_CUSTOM_SMALL = 0xBD,
      [System.ComponentModel.Description("MONARCH")]
      PAPER_CODE_MONARCH = 0xC2,
      [System.ComponentModel.Description("MONARCH_R")]
      PAPER_CODE_MONARCH_R = 0xC3,
      [System.ComponentModel.Description("DL")]
      PAPER_CODE_DL = 0xC4,
      [System.ComponentModel.Description("DL_R")]
      PAPER_CODE_DL_R = 0xC5,
      [System.ComponentModel.Description("C4")]
      PAPER_CODE_C4 = 0xC6,
      [System.ComponentModel.Description("C4_R")]
      PAPER_CODE_C4_R = 0xC7,
      [System.ComponentModel.Description("C5")]
      PAPER_CODE_C5 = 0xC8,
      [System.ComponentModel.Description("C5_R")]
      PAPER_CODE_C5_R = 0xC9,
      [System.ComponentModel.Description("C6")]
      PAPER_CODE_C6 = 0xCA,
      [System.ComponentModel.Description("C6_R")]
      PAPER_CODE_C6_R = 0xCB,
      [System.ComponentModel.Description("C65")]
      PAPER_CODE_C65 = 0xC,
      [System.ComponentModel.Description("C65_R")]
      PAPER_CODE_C65_R = 0xCD,
      [System.ComponentModel.Description("ISOB5")]
      PAPER_CODE_ISOB5 = 0xCE,
      [System.ComponentModel.Description("ISOB5_R")]
      PAPER_CODE_ISOB5_R = 0xCF,
      [System.ComponentModel.Description("SIZE6")]
      PAPER_CODE_SIZE6 = 0xD0,
      [System.ComponentModel.Description("SIZE6_R")]
      PAPER_CODE_SIZE6_R = 0xD1,
      [System.ComponentModel.Description("SIZE9")]
      PAPER_CODE_SIZE9 = 0xD2,
      [System.ComponentModel.Description("SIZE9_R")]
      PAPER_CODE_SIZE9_R = 0xD3,
      [System.ComponentModel.Description("COM10")]
      PAPER_CODE_COM10 = 0xD8,
      [System.ComponentModel.Description("COM10_R")]
      PAPER_CODE_COM10_R = 0xD9,
      [System.ComponentModel.Description("INCH_E")]
      PAPER_CODE_INCH_E = 0xDA,
      [System.ComponentModel.Description("INCH_L")]
      PAPER_CODE_INCH_L = 0xDB,
      [System.ComponentModel.Description("INCH_PANORAMA")]
      PAPER_CODE_INCH_PANORAMA = 0xD,
      [System.ComponentModel.Description("INCH_CARD_LARGE")]
      PAPER_CODE_INCH_CARD_LARGE = 0xDD,
      [System.ComponentModel.Description("INCH_PROOFPHOTO")]
      PAPER_CODE_INCH_PROOFPHOTO = 0xDE,
      [System.ComponentModel.Description("INCH_CARD_SMALL")]
      PAPER_CODE_INCH_CARD_SMALL = 0xDF,
      [System.ComponentModel.Description("UNKNOWN_240")]
      PAPER_CODE_UNKNOWN_240 = 0xF0,
      [System.ComponentModel.Description("JAM")]
      PAPER_CODE_JAM = 0xFF
    }

    public enum PaperTypes : int
    {
      [System.ComponentModel.Description("Not Available")]
      NULL = -1,
      [System.ComponentModel.Description("Auto")]
      BYTYPE_CODE_AUTO = 0x0,
      [System.ComponentModel.Description("Letterhead")]
      BYTYPE_CODE_LETTERHEAD = 0x1,
      [System.ComponentModel.Description("Punched")]
      BYTYPE_CODE_PUNCHED = 0x2,
      [System.ComponentModel.Description("Recycled Paper")]
      BYTYPE_CODE_RECYCLED = 0x3,
      [System.ComponentModel.Description("Color Paper")]
      BYTYPE_CODE_COLOR = 0x4,
      [System.ComponentModel.Description("Plain")]
      BYTYPE_CODE_PLAIN = 0x5,
      [System.ComponentModel.Description("Printed")]
      BYTYPE_CODE_PRINTED = 0x6,
      [System.ComponentModel.Description("Transparency")]
      BYTYPE_CODE_OHP = 0x7,
      [System.ComponentModel.Description("Heavy Paper")]
      BYTYPE_CODE_HEAVY = 0x8,
      [System.ComponentModel.Description("Label")]
      BYTYPE_CODE_LABEL = 0x9,
      [System.ComponentModel.Description("Envelope")]
      BYTYPE_CODE_ENVELOPE = 0xA,
      [System.ComponentModel.Description("Postcard")]
      BYTYPE_CODE_POSTCARD = 0xB,
      [System.ComponentModel.Description("Tab Paper")]
      BYTYPE_CODE_TAB = 0xC,
      [System.ComponentModel.Description("Thin Paper")]
      BYTYPE_CODE_THIN = 0xD,
      [System.ComponentModel.Description("User Type 1")]
      BYTYPE_CODE_USER1 = 0xE,
      [System.ComponentModel.Description("User Type 2")]
      BYTYPE_CODE_USER2 = 0xF,
      [System.ComponentModel.Description("User Type 3")]
      BYTYPE_CODE_USER3 = 0x10,
      [System.ComponentModel.Description("User Type 4")]
      BYTYPE_CODE_USER4 = 0x11,
      [System.ComponentModel.Description("User Type 5")]
      BYTYPE_CODE_USER5 = 0x12,
      [System.ComponentModel.Description("User Type 6")]
      BYTYPE_CODE_USER6 = 0x13,
      [System.ComponentModel.Description("User Type 7")]
      BYTYPE_CODE_USER7 = 0x14,
      [System.ComponentModel.Description("User Type 8")]
      BYTYPE_CODE_USER8 = 0x15,
      [System.ComponentModel.Description("User Type 9")]
      BYTYPE_CODE_USER9 = 0x16,
      [System.ComponentModel.Description("UNKNOWN_23")]
      BYTYPE_CODE_UNKNOWN_23 = 0x17,
      [System.ComponentModel.Description("UNKNOWN_24")]
      BYTYPE_CODE_UNKNOWN_24 = 0x18,
      [System.ComponentModel.Description("UNKNOWN_25")]
      BYTYPE_CODE_UNKNOWN_25 = 0x19,
      [System.ComponentModel.Description("UNKNOWN_26")]
      BYTYPE_CODE_UNKNOWN_26 = 0x20,
      [System.ComponentModel.Description("UNKNOWN_27")]
      BYTYPE_CODE_UNKNOWN_27 = 0x21,
      [System.ComponentModel.Description("UNKNOWN_28")]
      BYTYPE_CODE_UNKNOWN_28 = 0x22,
      [System.ComponentModel.Description("UNKNOWN_29")]
      BYTYPE_CODE_UNKNOWN_29 = 0x23,
      [System.ComponentModel.Description("Heavy Paper 2 (129-176g/m2)")]
      BYTYPE_CODE_HEAVY2 = 0x65,
      [System.ComponentModel.Description("Plain Paper 2")]
      BYTYPE_CODE_PLAIN2 = 0x66,
      [System.ComponentModel.Description("Heavy Paper 3 (177-205g/m2)")]
      BYTYPE_CODE_HEAVY3 = 0x67,
      [System.ComponentModel.Description("Heavy Paper 4 (206-300g/m2)")]
      BYTYPE_CODE_HEAVY4 = 0x68,
      [System.ComponentModel.Description("Glossy")]
      BYTYPE_CODE_GLOSSY = 0x69,
      [System.ComponentModel.Description("UNKNOWN_106")]
      BYTYPE_CODE_UNKNOWN_1 = 0x6A
    }

    public EventItem()
    {
    }

    [Newtonsoft.Json.JsonProperty("description")]
    public string Name { get; set; }

    [Newtonsoft.Json.JsonProperty("eventType")]
    public EventTypes EventType { get; set; }

    [Newtonsoft.Json.JsonProperty("code")]
    public string EventCode { get; set; }

    [Newtonsoft.Json.JsonProperty("dateTime")]
    public DateTime EventDateTime { get; set; }

    [Newtonsoft.Json.JsonProperty("paper")]
    public PaperTypes PaperType { get; set; }
    [Newtonsoft.Json.JsonProperty("totalCountBW")]
    public int BlackWhiteTotalCount { get; set; }

    [Newtonsoft.Json.JsonProperty("totalCountColor")]
    public int ColorTotalCount { get; set; }
  }

  public class Alert
  {
    public enum SeverityLevels : int
    {
      [System.ComponentModel.Description("Other")]
      Other = 1,
      [System.ComponentModel.Description("Critical")]
      Critical = 3,
      [System.ComponentModel.Description("Warning")]
      Warning = 4,
      [System.ComponentModel.Description("Warning Binary Change Events")]
      WarningBinaryChangeEvents = 5
    }

    public enum SubUnitTypes : int
    {
      // Values 5 to 29 reserved for Printer MIB
      [System.ComponentModel.Description("Other")]
      Other = 1,
      [System.ComponentModel.Description("Host Storage")]
      HostResourcesMIBStorageTable = 3,
      [System.ComponentModel.Description("Host Device")]
      HostResourcesMIBDeviceTable = 4,
      [System.ComponentModel.Description("General Printer")]
      GeneralPrinter = 5,
      [System.ComponentModel.Description("Cover")]
      Cover = 6,
      [System.ComponentModel.Description("Localization")]
      Localization = 7,
      [System.ComponentModel.Description("Input")]
      Input = 8,
      [System.ComponentModel.Description("Output")]
      Output = 9,
      [System.ComponentModel.Description("Marker")]
      Marker = 10,
      [System.ComponentModel.Description("Marker Supplies")]
      MarkerSupplies = 11,
      [System.ComponentModel.Description("Marker Colorant")]
      MarkerColorant = 12,
      [System.ComponentModel.Description("Media Path")]
      MediaPath = 13,
      [System.ComponentModel.Description("Channel")]
      Channel = 14,
      [System.ComponentModel.Description("Interpreter")]
      Interpreter = 15,
      [System.ComponentModel.Description("Console Display Buffer")]
      ConsoleDisplayBuffer = 16,
      [System.ComponentModel.Description("Console Lights")]
      ConsoleLights = 17,
      [System.ComponentModel.Description("Alert")]
      Alert = 18,
      // Values 30 to 39 reserved for Finisher MIB
      // Finisher sub units not implemented
      [System.ComponentModel.Description("Finisher Device")]
      [System.ComponentModel.Bindable(false)]
      FinDevice = 30,
      [System.ComponentModel.Description("Finisher Supply")]
      [System.ComponentModel.Bindable(false)]
      FinSupply = 31,
      [System.ComponentModel.Description("Finisher Supply Media Input")]
      [System.ComponentModel.Bindable(false)]
      FinSupplyMediaInput = 32,
      [System.ComponentModel.Description("Finisher Attribute")]
      [System.ComponentModel.Bindable(false)]
      FinAttribute = 33
    }

    public enum AlertCodes : int
    {
      [System.ComponentModel.Description("Other")]
      Other = 1,
      [System.ComponentModel.Description("Unknown")]
      Unknown = 2,
      [System.ComponentModel.Description("Cover Open")]
      CoverOpen = 3,
      [System.ComponentModel.Description("Cover Closed")]
      CoverClosed = 4,
      [System.ComponentModel.Description("Interlock Open")]
      InterlockOpen = 5,
      [System.ComponentModel.Description("Interlock Closed")]
      InterlockClosed = 6,
      [System.ComponentModel.Description("Configuration Change")]
      ConfigurationChange = 7,
      [System.ComponentModel.Description("Jam")]
      Jam = 8,
      [System.ComponentModel.Description("Sub unit Missing")]
      SubunitMissing = 9,  // The subunit tray, bin, etc. has been removed.
      [System.ComponentModel.Description("Sub unit Life Almost Over")]
      SubunitLifeAlmostOver = 10,
      [System.ComponentModel.Description("Sub unit Life Over")]
      SubunitLifeOver = 11,
      [System.ComponentModel.Description("Sub unit Almost Empty")]
      SubunitAlmostEmpty = 12,
      [System.ComponentModel.Description("Sub unit Empty")]
      SubunitEmpty = 13,
      [System.ComponentModel.Description("Sub unit Almost Full")]
      SubunitAlmostFull = 14,
      [System.ComponentModel.Description("Sub unit Full")]
      SubunitFull = 15,
      [System.ComponentModel.Description("Sub unit Near Limit")]
      SubunitNearLimit = 16,
      [System.ComponentModel.Description("Sub unit At Limit")]
      SubunitAtLimit = 17,
      [System.ComponentModel.Description("Sub unit Opened")]
      SubunitOpened = 18,
      [System.ComponentModel.Description("Sub unit Closed")]
      SubunitClosed = 19,
      [System.ComponentModel.Description("Sub unit Turned On")]
      SubunitTurnedOn = 20,
      [System.ComponentModel.Description("Sub unit Turned Off")]
      SubunitTurnedOff = 21,
      [System.ComponentModel.Description("Sub unit Offline")]
      SubunitOffline = 22,
      [System.ComponentModel.Description("Sub unit Power Saver")]
      SubunitPowerSaver = 23,
      [System.ComponentModel.Description("Sub unit Warming Up")]
      SubunitWarmingUp = 24,
      [System.ComponentModel.Description("Sub unit Added")]
      SubunitAdded = 25,
      [System.ComponentModel.Description("Sub unit Removed")]
      SubunitRemoved = 26,
      [System.ComponentModel.Description("Sub unit Resource Added")]
      SubunitResourceAdded = 27,
      [System.ComponentModel.Description("Sub unit Resource Removed")]
      SubunitResourceRemoved = 28,
      [System.ComponentModel.Description("Sub unit Recoverable Failure")]
      SubunitRecoverableFailure = 29,
      [System.ComponentModel.Description("Sub unit Unrecoverable Failure")]
      SubunitUnrecoverableFailure = 30,
      [System.ComponentModel.Description("Sub unit Recoverable Storage Error")]
      SubunitRecoverableStorageError = 31,
      [System.ComponentModel.Description("Sub unit Unrecoverable Storage Error")]
      SubunitUnrecoverableStorageError = 32,
      [System.ComponentModel.Description("Sub unit Motor Failure")]
      SubunitMotorFailure = 33,
      [System.ComponentModel.Description("Sub unit Memory Exhausted")]
      SubunitMemoryExhausted = 34,
      [System.ComponentModel.Description("Sub unit Under Temperature")]
      SubunitUnderTemperature = 35,
      [System.ComponentModel.Description("Sub unit Over Temperature")]
      SubunitOverTemperature = 36,
      [System.ComponentModel.Description("Sub unit Timing Failure")]
      SubunitTimingFailure = 37,
      [System.ComponentModel.Description("Sub unit Thermistor Failure")]
      SubunitThermistorFailure = 38,
      // General Printer group
      // doorOpen = 501 - -DEPRECATED - Use coverOpened = 3
      // doorClosed = 502 -- DEPRECATED - Use coverClosed = 4
      [System.ComponentModel.Description("Power Up")]
      PowerUp = 503,
      [System.ComponentModel.Description("Power Down")]
      PowerDown = 504,
      [System.ComponentModel.Description("Printer NMS Reset")]
      PrinterNMSReset = 505,
      [System.ComponentModel.Description("Printer Manual Reset")]
      PrinterManualReset = 506,
      [System.ComponentModel.Description("Printer Ready to Print")]
      PrinterReadyToPrint = 507,
      [System.ComponentModel.Description("Input Media Tray Missing")]
      InputMediaTrayMissing = 801,
      [System.ComponentModel.Description("Input Media Size Change")]
      InputMediaSizeChange = 802,
      [System.ComponentModel.Description("Input Media Weight Change")]
      InputMediaWeightChange = 803,
      [System.ComponentModel.Description("Input Media Type Change")]
      InputMediaTypeChange = 804,
      [System.ComponentModel.Description("Input Media Color Change")]
      InputMediaColorChange = 805,
      [System.ComponentModel.Description("Input Media Form Parts Change")]
      InputMediaFormPartsChange = 806,
      [System.ComponentModel.Description("Input Media Supply Low")]
      InputMediaSupplyLow = 807,
      [System.ComponentModel.Description("Input Media Supply Empty")]
      InputMediaSupplyEmpty = 808,
      [System.ComponentModel.Description("Input Media Change Request")]
      InputMediaChangeRequest = 809,
      [System.ComponentModel.Description("Input Manual Input Request")]
      InputManualInputRequest = 810,
      [System.ComponentModel.Description("Input Tray Position Failure")]
      InputTrayPositionFailure = 811,
      [System.ComponentModel.Description("Input Tray Elevation Failure")]
      InputTrayElevationFailure = 812,
      [System.ComponentModel.Description("Input Cannot Feed Size Selected")]
      InputCannotFeedSizeSelected = 813,
      [System.ComponentModel.Description("Output Media Tray Missing")]
      OutputMediaTrayMissing = 901,
      [System.ComponentModel.Description("Output Media Tray Almost Full")]
      OutputMediaTrayAlmostFull = 902,
      [System.ComponentModel.Description("Output Media Tray Full")]
      OutputMediaTrayFull = 903,
      [System.ComponentModel.Description("Output Mailbox Select Failure")]
      OutputMailboxSelectFailure = 904,
      [System.ComponentModel.Description("Marker Fuser Under Temperature")]
      MarkerFuserUnderTemperature = 1001,
      [System.ComponentModel.Description("Marker Fuser Over Temperature")]
      MarkerFuserOverTemperature = 1002,
      [System.ComponentModel.Description("Marker Fuser Timing Failure")]
      MarkerFuserTimingFailure = 1003,
      [System.ComponentModel.Description("Marker Fuser Thermistor Failure")]
      MarkerFuserThermistorFailure = 1004,
      [System.ComponentModel.Description("Marker Adjusting Print Quality")]
      MarkerAdjustingPrintQuality = 1005,
      [System.ComponentModel.Description("Marker Toner Empty")]
      MarkerTonerEmpty = 1101,
      [System.ComponentModel.Description("Marker Ink Empty")]
      MarkerInkEmpty = 1102,
      [System.ComponentModel.Description("Marker Print Ribbon Empty")]
      MarkerPrintRibbonEmpty = 1103,
      [System.ComponentModel.Description("Marker Toner Almost Empty")]
      MarkerTonerAlmostEmpty = 1104,
      [System.ComponentModel.Description("Marker Ink Almost Empty")]
      MarkerInkAlmostEmpty = 1105,
      [System.ComponentModel.Description("Marker Print Ribbon Almost Empty")]
      MarkerPrintRibbonAlmostEmpty = 1106,
      [System.ComponentModel.Description("Marker Waste Toner Receptacle Almost Full")]
      MarkerWasteTonerReceptacleAlmostFull = 1107,
      [System.ComponentModel.Description("Marker Waste Ink Receptacle Almost Full")]
      MarkerWasteInkReceptacleAlmostFull = 1108,
      [System.ComponentModel.Description("Marker Waste Toner Receptacle Full")]
      MarkerWasteTonerReceptacleFull = 1109,
      [System.ComponentModel.Description("Marker Waste Ink Receptacle Full")]
      MarkerWasteInkReceptacleFull = 1110,
      [System.ComponentModel.Description("Marker OPC Life Almost Over")]
      MarkerOpcLifeAlmostOver = 1111,
      [System.ComponentModel.Description("Marker OPC Life Over")]
      MarkerOpcLifeOver = 1112,
      [System.ComponentModel.Description("Marker Developer Almost Empty")]
      MarkerDeveloperAlmostEmpty = 1113,
      [System.ComponentModel.Description("Marker Developer Empty")]
      MarkerDeveloperEmpty = 1114,
      [System.ComponentModel.Description("Marker Toner Cartridge Missing")]
      MarkerTonerCartridgeMissing = 1115,
      [System.ComponentModel.Description("Media Path Media Tray Missing")]
      MediaPathMediaTrayMissing = 1301,
      [System.ComponentModel.Description("Media Path Media Tray Almost Full")]
      MediaPathMediaTrayAlmostFull = 1302,
      [System.ComponentModel.Description("Media Path Media Tray Full")]
      MediaPathMediaTrayFull = 1303,
      [System.ComponentModel.Description("Media Path Cannot Dupley Media Selected")]
      MediaPathCannotDuplexMediaSelected = 1304,
      [System.ComponentModel.Description("Interpreter Memory Increase")]
      InterpreterMemoryIncrease = 1501,
      [System.ComponentModel.Description("Interpreter Memory Decrease")]
      InterpreterMemoryDecrease = 1502,
      [System.ComponentModel.Description("Interpreter Cartridge Added")]
      InterpreterCartridgeAdded = 1503,
      [System.ComponentModel.Description("Interpreter Cartridge Deleted")]
      InterpreterCartridgeDeleted = 1504,
      [System.ComponentModel.Description("Interpreter Resource Added")]
      InterpreterResourceAdded = 1505,
      [System.ComponentModel.Description("Interpreter Resource Deleted")]
      InterpreterResourceDeleted = 1506,
      [System.ComponentModel.Description("Interpreter Resource Unavailable")]
      InterpreterResourceUnavailable = 1507,
      [System.ComponentModel.Description("Interpreter Complex Page Encountered")]
      InterpreterComplexPageEncountered = 1509,
      [System.ComponentModel.Description("Alert Removal Of Binary Change Entry")]
      AlertRemovalOfBinaryChangeEntry = 1801
    }

    public Alert()
    {
    }

    [Newtonsoft.Json.JsonProperty("severityLevel")]
    public Alert.SeverityLevels SeverityLevel { get; set; }

    [Newtonsoft.Json.JsonProperty("type")]
    public Alert.SubUnitTypes SubUnitType { get; set; }

    [Newtonsoft.Json.JsonProperty("subUnit")]
    public string SubUnitName { get; set; }

    [Newtonsoft.Json.JsonProperty("alertCode")]
    public Alert.AlertCodes AlertCode { get; set; }

    [Newtonsoft.Json.JsonProperty("description")]
    public string Description { get; set; }

    [Newtonsoft.Json.JsonProperty("dateTime")]
    public DateTime DateTimeOccurred { get; set; }
  }


  [Newtonsoft.Json.JsonObject("telemetry")]
  public class Telemetry
  {
    //Telemetry()
    //{
    //}

    [Newtonsoft.Json.JsonProperty("lastRedTransactionDate", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
    public DateTime LastRedTransactionDate { get; set; }

    [Newtonsoft.Json.JsonProperty("lastAgentTransactionDate", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
    public DateTime LastAgentTransactionDate { get; set; }
  }


  [Newtonsoft.Json.JsonObject("total")]
  public class Total
  {
    public Total()
    {
    }

    public Total(Counter.JobColorTypes ColorType, DateTime ReadDate)
    {
      this.JobColorTypeIndex = ColorType;
      this.ReadDate = ReadDate;
    }
    
    [Newtonsoft.Json.JsonProperty("count")]
    public int Count { get; set; } = 0;

    [Newtonsoft.Json.JsonProperty("color")]
    public Counter.JobColorTypes JobColorTypeIndex;

    [Newtonsoft.Json.JsonProperty("readingDate")]
    public DateTime ReadDate;

  }

  public class TotalList : IList<Total>
  {
    private System.Collections.Generic.SortedDictionary<Counter.JobColorTypes, Total> mobjData = new();

    public TotalList()
    {
    }

    public void Add(Total Item)
    {
      mobjData.Add(Item.JobColorTypeIndex, Item);
    }

    public int IndexOf(Total item)
    {
      throw new NotImplementedException();
    }

    public void Insert(int index, Total item)
    {
      throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
      throw new NotImplementedException();
    }

    public void Clear()
    {
      mobjData.Clear();
    }

    public bool Contains(Total item)
    {
      return mobjData.ContainsKey(item.JobColorTypeIndex);
    }

    public void CopyTo(Total[] array, int arrayIndex)
    {
      mobjData.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(Total item)
    {
      return mobjData.Remove(item.JobColorTypeIndex);
    }

    public IEnumerator<Total> GetEnumerator()
    {
      return mobjData.Values.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return mobjData.Values.GetEnumerator();
    }

    // public new Total Item[Counter.JobColorTypes ColorType]
    public Total this[Counter.JobColorTypes ColorType]
    {
      get
      {
        if (!mobjData.ContainsKey(ColorType))
          mobjData.Add(ColorType, new Total(ColorType, DateTime.MinValue));

        return mobjData[ColorType];
      }
      set
      {
        if (!mobjData.ContainsKey(ColorType))
          mobjData.Add(ColorType, new Total());

        mobjData[ColorType] = value;
      }
    }

    public Total this[Counter.JobColorTypes ColorType, bool AutoAdd]
    {
      get
      {
        if (!mobjData.ContainsKey(ColorType))
          return new Total(ColorType, DateTime.MinValue);

        return mobjData[ColorType];
      }
      set
      {
        if (!mobjData.ContainsKey(ColorType))
          mobjData.Add(ColorType, new Total());

        mobjData[ColorType] = value;
      }
    }

    public Total this[int index]
    {
      get
      {
        Total[] values = new Total[mobjData.Values.Count];

        mobjData.Values.CopyTo(values, 0);

        //todo! work out the collection object
        //System.Collections.Generic.SortedDictionary<Counter.JobColorTypes, Total>.ValueCollection values = mobjData.Values;
        //return values[1];


        return values[index];
      }
      set
      {
      }
    }

    public int Count
    {
      get
      {
        return mobjData.Count;
      }
    }

    public bool IsReadOnly
    {
      get
      {
        return false;
      }
    }
  }
  
  [Newtonsoft.Json.JsonObject("tonerLevels")]
  public class TonerLevel
  {
    public enum TonerLowFlags : int
    {
      Normal = 0,
      Low = 1,
      UseTonerNearEndFlag = 2
    }

    public enum TonerTypes : int
    {
      [System.ComponentModel.Description("N/A")]
      [System.ComponentModel.Bindable(false)]
      NotApplicable = -1,
      Black = 0,
      Cyan = 1,
      Magenta = 2,
      Yellow = 3,
      Clear = 4,
      White = 5,
      Gold = 6,
      Silver = 7,
      Pink = 8,
      Textured = 9
    }

    public TonerLevel()
    {
    }

    [Newtonsoft.Json.JsonProperty("type")]
    public TonerTypes TonerType { get; set; }

    [Newtonsoft.Json.JsonProperty("tonerNumber")]
    public int TonerNumber { get; set; } = int.MinValue;

    [Newtonsoft.Json.JsonProperty("tonerRemaining")]
    public int TonerRemaining { get; set; } = -1;

    [Newtonsoft.Json.JsonProperty("printCount")]
    public int PrintCount { get; set; }

    [Newtonsoft.Json.JsonProperty("dateTime")]
    public DateTime DateTime { get; set; }

    [Newtonsoft.Json.JsonProperty("forecastPrintCount")]
    [DefaultValue(-1)]
    public int ForecastPrintCount { get; set; }

    [Newtonsoft.Json.JsonProperty("forecastDateTime", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore)]
    public DateTime ForecastDateTime { get; set; }

    [Newtonsoft.Json.JsonProperty("sKU")]
    [DefaultValue("")]
    public string SKU { get; set; }
  }


  public class MaintenanceTaskList : System.Collections.ObjectModel.KeyedCollection<Guid, MaintenanceTask>
  {
    public bool Exists(Guid ID)
    {
      return base.Contains(ID);
    }

    protected override System.Guid GetKeyForItem(MaintenanceTask item)
    {
      return item.ID;
    }
  }

  [Newtonsoft.Json.JsonObject("maintenanceTask")]
  public class MaintenanceTask
  {
    public MaintenanceTask()
    {
    }

    [Newtonsoft.Json.JsonIgnore()]
    public Guid ID { get; set; }

    [Newtonsoft.Json.JsonProperty("name")]
    public string Name { get; set; }

    [Newtonsoft.Json.JsonProperty("code")]
    public string Code { get; set; }

    [Newtonsoft.Json.JsonProperty("type")]
    public string Type { get; set; }

    [Newtonsoft.Json.JsonProperty("description")]
    public string Description { get; set; }
    [Newtonsoft.Json.JsonProperty("action")]
    public string Action { get; set; }

    [Newtonsoft.Json.JsonProperty("parts")]
    public List<Part> Parts { get; set; } = new List<Part>();

    public bool ShouldSerializeParts()
    {
      return this.Parts.Count != 0;
    }
  }


  [Newtonsoft.Json.JsonObject("part")]
  public class Part
  {
    public Part()
    {
    }

    [Newtonsoft.Json.JsonProperty("name")]
    public string Name { get; set; }
    [Newtonsoft.Json.JsonProperty("description")]
    public string Description { get; set; }
    [Newtonsoft.Json.JsonProperty("quantity")]
    public int Quantity { get; set; }
    [Newtonsoft.Json.JsonProperty("code")]
    public string Code { get; set; }
  }

  [Newtonsoft.Json.JsonObject("supplyLevel")]
  public class SupplyLevel
  {
    public enum SupplyTypes : int
    {
      Other=1,
      // markersuppliesType attribute must match the prtMarkerSuppliesType (case sensitive)
      // <MarkerSuppliesType("toner")> Toner = 3
      [System.ComponentModel.Description("Toner")]
      [System.ComponentModel.Bindable(false)]
      Toner = 3,
      [System.ComponentModel.Description("Waste Toner Container")]
      WasteToner = 4,
      [System.ComponentModel.Description("OPC Drum")]
      OPC = 9,
      [System.ComponentModel.Description("Developer Drum")]
      Developer = 10,
      [System.ComponentModel.Description("Fuser")]
      Fuser = 15,
      [System.ComponentModel.Description("Transfer Unit")]
      TransferUnit = 20,
      [System.ComponentModel.Description("Toner Cartridge")]
      TonerCartridge = 21,
      [System.ComponentModel.Description("Fuser Oiler")]
      FuserOiler = 22,
      [System.ComponentModel.Description("Staple")]
      Staple = 32,
      [System.ComponentModel.Description("Inserts")]
      Inserts = 33,
      [System.ComponentModel.Description("Covers")]
      Covers = 34
    }

    public enum ClassTypes : int
    {
      Container = 3,
      Receptacle = 4
    }

    SupplyLevel()
    {
    }

    [Newtonsoft.Json.JsonProperty("sKU")]
    [DefaultValue("")]
    public string SKU { get; set; }

    [Newtonsoft.Json.JsonProperty("classType")]
    public ClassTypes ClassType { get; set; }
    [Newtonsoft.Json.JsonProperty("supplyType")]
    public SupplyTypes SupplyType { get; set; }

    [Newtonsoft.Json.JsonProperty("colorType")]
    public TonerLevel.TonerTypes ColorType { get; set; }

    [Newtonsoft.Json.JsonProperty("value")]
    public int Value { get; set; }

    [Newtonsoft.Json.JsonProperty("supplyNumber")]
    public int SupplyNumber { get; set; } = int.MinValue;

    [Newtonsoft.Json.JsonProperty("datetime")]
    public DateTime DateTime { get; set; }
  }


  [Newtonsoft.Json.JsonObject("firmware")]
  public class Firmware
  {
    public Firmware()
    {
    }

    [Newtonsoft.Json.JsonProperty("currentVersion")]
    public string CurrentVersion { get; set; }

    [Newtonsoft.Json.JsonProperty("updateAvailable")]
    public FirmwareUpdate UpdateAvailable { get; set; }
  }

  [Newtonsoft.Json.JsonObject("firmware")]
  public class FirmwareUpdate
  {
    public FirmwareUpdate()
    {
    }

    [Newtonsoft.Json.JsonProperty("id")]
    public Guid Id { get; set; }

    [Newtonsoft.Json.JsonProperty("version")]
    public string Version { get; set; }

    [Newtonsoft.Json.JsonProperty("description")]
    public string Description { get; set; }
  }

  public class Counter
  {
    public enum JobCounterTypes : int
    {
      [System.ComponentModel.Description("Copy")]
      Copy = 0,
      [System.ComponentModel.Description("Print")]
      Print = 1,
      [System.ComponentModel.Description("Scan")]
      Scan = 2,
      [System.ComponentModel.Description("Scan Saved")]
      ScanSaved = 3,
      [System.ComponentModel.Description("Scan Sent")]
      ScanSent = 4,
      [System.ComponentModel.Description("Document File")]
      DocFile = 5,
      [System.ComponentModel.Description("USB")]
      USB = 6,
      [System.ComponentModel.Description("SMB Sent")]
      SMB = 7,
      [System.ComponentModel.Description("Network Scanner")]
      NetworkScanner = 8,
      [System.ComponentModel.Description("Maintenance")]
      Maintenance = 9,
      [System.ComponentModel.Description("Emails Sent")]
      EmailSent = 10,
      [System.ComponentModel.Description("Other Print")]
      OtherPrint = 11,
      [System.ComponentModel.Description("Trial")]
      Trial = 12,
      [System.ComponentModel.Description("I-Fax Received")]
      IFaxReceived = 13,
      [System.ComponentModel.Description("I-Fax Sent")]
      IFaxSent = 14,
      [System.ComponentModel.Description("IP-Fax Received")]
      IPFaxReceived = 15,
      [System.ComponentModel.Description("IP-Fax Sent")]
      IPFaxSent = 16,
      [System.ComponentModel.Description("FAX Sent")]
      FAXSent = 17,
      [System.ComponentModel.Description("FAX Received")]
      FAXReceived = 18,
      [System.ComponentModel.Description("FTP Sent")]
      FTPSent = 19,
      [System.ComponentModel.Description("FTP Received")]
      FTPReceived = 20,
      [System.ComponentModel.Description("Output Paper")]
      OutputPaper = 21,
      [System.ComponentModel.Description("Output Sheets")]
      OutputSheets = 22,
      [System.ComponentModel.Description("Available Paper")]
      AvailablePaper = 23,
      [System.ComponentModel.Description("Zaurus Print")]
      ZaurusPrint = 24,
      [System.ComponentModel.Description("FAX Sending Images")]
      FAXSendingImages = 25,
      [System.ComponentModel.Description("I-FAX Scanning Counter")]
      IFAXScanning = 26,
      [System.ComponentModel.Description("I-FAX Counter")]
      IFAXCounter = 27,
      [System.ComponentModel.Description("ImageSendSIM IFAXSent")]
      ImageSendSIMIFAXSent = 28,
      [System.ComponentModel.Description("ImageSendSIM IFAXReceived")]
      ImageSendSIMIFAXReceived = 29,
      [System.ComponentModel.Description("ImageSendSIM IPFAXSent")]
      ImageSendSIMIPFAXSent = 30,
      [System.ComponentModel.Description("ImageSendSIM IPFAXReceived")]
      ImageSendSIMIPFAXReceived = 31,
      [System.ComponentModel.Description("FAX Sent Line1")]
      FAXSentLine1 = 32,
      [System.ComponentModel.Description("FAX Sent Line2")]
      FAXSentLine2 = 33,
      [System.ComponentModel.Description("FAX Received Line1")]
      FAXReceivedLine1 = 34,
      [System.ComponentModel.Description("FAX Received Line2")]
      FAXReceivedLine2 = 35
    }

    public enum JobColorTypes : int
    {
      [System.ComponentModel.Description("n/a")]
      NULL = -1,
      [System.ComponentModel.Description("Auto")]
      Auto = 0,
      [System.ComponentModel.Description("Monochrome")]
      Monochrome = 1,
      [System.ComponentModel.Description("Single Color")]
      SingleColor = 2,
      [System.ComponentModel.Description("Dual Color")]
      DualColor = 3,
      [System.ComponentModel.Description("Triple Color")]
      TripleColor = 4,
      [System.ComponentModel.Description("Full Color")]
      FullColor = 5,
      [System.ComponentModel.Description("Grayscale")]
      Grayscale = 6
    }

    public Counter()
    {
    }

    [Newtonsoft.Json.JsonProperty("count")]
    public int Count { get; set; }

    [Newtonsoft.Json.JsonProperty("type")]
    public Counter.JobCounterTypes JobCounterTypeIndex;

    [Newtonsoft.Json.JsonProperty("color")]
    public Counter.JobColorTypes JobColorTypeIndex;

    [Newtonsoft.Json.JsonProperty("read-date")]
    public DateTime ReadDate;
  }

  public class TonerLevelHistoryItem
  {
    public TonerLevelHistoryItem()
    {
    }

    [Newtonsoft.Json.JsonProperty("type")]
    public TonerLevel.TonerTypes TonerType { get; set; }

    [Newtonsoft.Json.JsonProperty("tonerNumber")]
    public int TonerNumber { get; set; } = int.MinValue;

    [Newtonsoft.Json.JsonProperty("tonerLow")]
    public TonerLevel.TonerLowFlags TonerLow { get; set; } = TonerLevel.TonerLowFlags.Normal;

    [Newtonsoft.Json.JsonProperty("tonerRemaining")]
    public int TonerRemaining { get; set; } = -1;

    [Newtonsoft.Json.JsonProperty("printCount")]
    public int PrintCount { get; set; }

    [Newtonsoft.Json.JsonProperty("dateTime")]
    public DateTime DateTime { get; set; }

    [Newtonsoft.Json.JsonIgnore()]
    public bool IsFirstEntry { get; set; }
  }

}
