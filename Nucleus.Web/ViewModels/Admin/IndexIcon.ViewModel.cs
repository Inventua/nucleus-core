namespace Nucleus.Web.ViewModels.Admin
{
  public class IndexIcon
  {
    public string Id { get; set; } = null;
    public string Title { get; set; }
    public string Tooltip { get; set; }
    public string Url { get; set; }
    public string AccessKey { get; set; }
    public string Glyph { get; set; }
    public string ClassName { get; set; }

    public IndexIcon(string title, string tooltip, string url, string accesskey, string glyph, string className)
		{
      this.Title = title;
      this.Tooltip = tooltip;
      this.Url = url;
      this.AccessKey = accesskey;
      this.Glyph = glyph;
      this.ClassName = className;
    }

    public IndexIcon(string id, string title, string tooltip, string url, string accesskey, string glyph, string className)
      : this(title, tooltip, url, accesskey, glyph, className)
    {
      this.Id = id;      
    }
  }
}
