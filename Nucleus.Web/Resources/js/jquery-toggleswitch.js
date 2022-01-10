(function ($)
{
  jQuery.fn.ToggleSwitch = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
    }, conf);

    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      // initialize 
      targetControl = jQuery(this);

      if (!targetControl.is('input[type=checkbox]')) return;

      // check to see if the toggleswitch is already initialized
      if (targetControl.siblings('.ToggleSwitch').length !== 0) return;

      switchElement = jQuery('<input type="range" min="1" max="2" step="1" class="ToggleSwitch"></input>');
      targetControl.hide();
      switchElement.attr('value', targetControl.is(':checked') ? "2" : "1");
      switchElement.addClass(targetControl.is(':checked') ? "checked" : "");

      // handle clicks on the toggle control (range control)
      switchElement.on('click', function ()
      {
        targetControl = jQuery(this).siblings('input[type=checkbox]');
        toggle(targetControl, jQuery(this));
      });

      // handle clicks on an associated label control
      targetControl.on('click', function ()
      {
        toggleControl = jQuery(this).siblings('.ToggleSwitch');
        toggleControl.val(toggleControl.val() === '1' ? '2' : '1');
        toggle(jQuery(this), toggleControl);
      });

      targetControl.after(switchElement);

    });    
  }

  function toggle(targetControl, toggleswitchControl)
  {
    targetControl.prop('checked', toggleswitchControl.val() > 1);
    targetControl.trigger('change');

    if (toggleswitchControl.val() > 1)
    {
      toggleswitchControl.addClass('checked');
    }
    else
    {
      toggleswitchControl.removeClass('checked');
    }
	}
})(jQuery);