namespace Nucleus.Web.ViewModels.Admin
{
  public class ManageIcon
  {
    public string Title { get; set; }
    public string Caption { get; set; }
    public string Url { get; set; }
    public string AccessKey { get; set; }
    public string Glyph { get; set; }
    public string ClassName { get; set; }
    public string Summary { get; set; }

    public ManageIcon(string title, string caption, string summary, string url, string accesskey, string glyph, string className)
		{
      this.Title = title;
      this.Caption = caption;
      this.Summary = summary;
      this.Url = url;
      this.AccessKey = accesskey;
      this.Glyph = glyph;
      this.ClassName = className;
    }

  }
}
