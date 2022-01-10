(function ($)
{
  jQuery.fn.PageList = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
    }, conf);

    // allow clicks outside the control to close it
    jQuery(document).on('click', function (event)
    {
      jQuery('.nucleus-page-list ul').removeClass('show');

      jQuery('.nucleus-page-list').each(function (index, element)
      {
        var control = jQuery(element);
        if (control.is(event.target) || control.has(event.target).length !== 0)
        {
          control.find('ul:first').addClass('show');
        }
      });
    });

    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      // initialize 
      control = jQuery(this);

      control.find('.nucleus-page-list-selected, button').on('click', function (event)
      {
        jQuery(this).siblings('ul').addClass('show');
        event.preventDefault();
      });

      // we must use a jquery delegated event handler, because the list can expand (have more elements added)
      control.on('click', 'a', function (event)
      {
        event.preventDefault();
        event.stopPropagation();

        jQuery(this).siblings().removeClass("selected");
        jQuery(this).addClass("selected");
        jQuery(this).closest('.nucleus-page-list').find('input[type=hidden]').val(jQuery(this).attr('data-id'));
        jQuery(this).closest('.nucleus-page-list').find('.nucleus-page-list-selected').html(jQuery(this).html());

        jQuery(this).closest('.nucleus-page-list').find('ul:first').removeClass('show');
      });

      
    });
  }

})(jQuery);