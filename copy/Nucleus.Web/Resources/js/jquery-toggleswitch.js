////@Html.CheckBoxFor(model => model.Page.DisableInMenu, new { @id= "TEST", @class = "ToggleButtonControl" })
////<input type="range" min="1" max="2" value="1" step="1" style="width: 30px" onchange="jQuery('#TEST').prop('checked', this.value > 1);">
(function ($)
{
  jQuery.fn.ToggleSwitch = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
      dataTheme: 'a',
      dataContentTheme: 'b',
      data: null
    }, conf);

    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      if (!jQuery(this).is('input[type=checkbox]')) return;      

      // initialize 
      targetControl = jQuery(this);
      switchElement = jQuery('<input type="range" min="1" max="2" step="1" class="ToggleSwitch"></input>');
      targetControl.hide();
      switchElement.attr('value', targetControl.is(':checked') ? "2" : "1");
      switchElement.addClass(targetControl.is(':checked') ? "checked" : "");

      switchElement.on('click', function ()
      {
        targetControl = jQuery(this).siblings('input[type=checkbox]');
        targetControl.prop('checked', jQuery(this).val() > 1);
        targetControl.trigger('change');

        if (jQuery(this).val() > 1)
        {
          jQuery(this).addClass('checked');
        }
        else
        {
          jQuery(this).removeClass('checked');
				}
      });

      targetControl.after(switchElement);

    });
  }

})(jQuery);