/*! jquery-toggleswitch | Provides a toggle switch in place of a checkbox, part of Nucleus CMS.  (c) Inventua Pty Ptd.  www.nucleus-cms.com */
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
      if (targetControl.next().is('input[type=range]')) return;

      switchElement = jQuery('<input type="range" min="1" max="2" step="1"></input>');
      switchElement.addClass(targetControl.attr('class'));
      switchElement.prop('disabled', targetControl.prop('disabled'));
      targetControl.hide();
      switchElement.attr('value', targetControl.is(':checked') ? "2" : "1");
      switchElement.addClass(targetControl.is(':checked') ? "checked" : "");

      // handle clicks on the toggle control (range control)
      switchElement.off('.toggleswitch');
      switchElement.on('change.toggleswitch input.toggleswitch click.toggleswitch', function (event)
      {
        event.preventDefault();
        event.stopImmediatePropagation();
        targetControl = jQuery(this).prev();
        if (!(targetControl).is('input[type=checkbox]')) return;
        toggle(targetControl, jQuery(this));
      });

      // handle clicks on an associated label control
      targetControl.off('.toggleswitch');
      targetControl.on('click.toggleswitch', function (event)
      {
        event.preventDefault();
        event.stopImmediatePropagation();

        toggleControl = jQuery(this).next();
        if (!(toggleControl).is('input[type=range]')) return;

        toggleControl.val(toggleControl.val() === '1' ? '2' : '1');
        toggle(jQuery(this), toggleControl);
      });

      targetControl.after(switchElement);
    });    
  }

  function toggle(targetControl, toggleswitchControl)
  {
    if (targetControl.is(':checked') !== toggleswitchControl.val() > 1)
    {
      targetControl.prop('checked', toggleswitchControl.val() > 1);
      toggleswitchControl.toggleClass('checked', toggleswitchControl.val() > 1);
      targetControl.trigger('change');
    }
	}
})(jQuery);