/*! .  (c) Inventua Pty Ptd.  www.nucleus-cms.com */
(function ($)
{
  jQuery.fn.NormalizeItemHeights = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
    }, conf);

    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      // initialize 
      carouselControl = jQuery(this);

      if (!carouselControl.is('.carousel')) return;

      _normalizeHeights(carouselControl);

      jQuery(window).on('resize', function ()
      {
        _normalizeHeights(carouselControl);
      });
    });

    function _normalizeHeights(target)
    {
      var tallestHeight = 0;
      target.find('.carousel-item').css('height', '');

      // normalize carousel item heights
      target.find('.carousel-item').each(function (index, element) 
      {
        let wasMadeActive = false;
        if (!jQuery(element).hasClass('active'))
        {
          jQuery(element).addClass("active");
          wasMadeActive = true;
        }
        else
        {
          wasMadeActive = false;
        }

        let elementHeight = jQuery(element).height();
        if (elementHeight > tallestHeight)
        {
          tallestHeight = elementHeight;
        }

        if (wasMadeActive)
        {
          jQuery(element).removeClass("active");
        }
      });

      target.find('.carousel-item').height(tallestHeight);
    }
  }

})(jQuery);