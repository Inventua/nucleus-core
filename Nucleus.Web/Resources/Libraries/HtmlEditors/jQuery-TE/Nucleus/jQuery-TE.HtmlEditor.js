(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {
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
        strike: false
      });
    });  }
})(jQuery);

