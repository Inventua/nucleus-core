(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {
    var isAdminMode = conf == null || typeof (conf.isAdminMode) === 'undefined' ? false : conf.isAdminMode;

    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      var htmlEditor = jQuery(value).jqte({
        fsize: false,
        color: false,
        sub: false,
        sup: false,
        indent: false,
        outdent: false,
        strike: false,
        source: isAdminMode
      });
    });  }
})(jQuery);

