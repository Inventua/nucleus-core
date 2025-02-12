﻿/*! jquery-pagelist | Provides a page list expandable drop-down for admin pages in Nucleus CMS.  (c) Inventua Pty Ptd.  www.nucleus-cms.com */
(function ($)
{
  jQuery.fn.PageList = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
    }, conf);

    // allow clicks outside the control to close the page list
    jQuery(document).on('click', function (event)
    {
      jQuery('.nucleus-page-list ul').removeClass('show');

      jQuery('.nucleus-page-list').each(function (index, element)
      {
        var control = jQuery(element);
        if (control.is(event.target) || control.has(event.target).length !== 0)
        {
          control.find('ul:first').addClass('show');
          control.find('ul:first').find('a:first').focus();
        }
      });
    });

    // allow up arrow or page up to close page list
    jQuery(document).on('keydown', function (event)
    {
      if (event.which === 38 || event.which === 33)
      {
        jQuery('.nucleus-page-list ul').removeClass('show');
      }
    });


    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      // initialize 
      control = jQuery(this);

      control.find('.nucleus-page-list-selected, button').on('click', function (event)
      {
        jQuery(this).siblings('ul').addClass('show');

        // Bring the selected page into view
        window.setTimeout(function ()
        {
          var selectedPageItem = jQuery('.nucleus-page-list li.selected');
          if (selectedPageItem.length !== 0)
          {
            selectedPageItem[0].scrollIntoView({ block: "nearest", inline: "nearest" });
          }
        }, 100);

        event.preventDefault();
      });

      control.find('.nucleus-page-list-selected, button').on('keydown', function (event)
      {
        // trigger click event when the user presses space or down arrow or page down
        if (event.which === 32 || event.which === 40 || event.which === 34)
        {
          jQuery(this.click());
          event.preventDefault();
				}
      });

      // we must use a jquery delegated event handler, because the list can expand (have more elements added)
      control.on('click', 'a', function (event)
      {
        event.preventDefault();
        event.stopPropagation();

        jQuery(event.currentTarget).parents('ul, ol').find('LI.selected').removeClass('selected');
        if (jQuery(event.currentTarget).is('li'))
        {
          jQuery(event.currentTarget).addClass('selected');
        }
        else
        {
          jQuery(event.currentTarget).parent('li').addClass('selected');
        }
        jQuery(this).closest('.nucleus-page-list').find('input[type=hidden]').val(jQuery(this).attr('data-id'));
        jQuery(this).closest('.nucleus-page-list').find('input[type=hidden]').trigger('change');
        jQuery(this).closest('.nucleus-page-list').find('.nucleus-page-list-selected').html(jQuery(this).html());

        jQuery(this).closest('.nucleus-page-list').find('ul:first').removeClass('show');
      });

      
    });
  }

})(jQuery);