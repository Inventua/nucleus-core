(function ($)
{
  jQuery.fn.Recaptcha = function (conf)
  {
    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      let siteKey = jQuery(value).attr('data-verification-site-key');
      var badgeElement = jQuery(jQuery(value).attr('data-verification-badge-element-selector'));

      let clientId = grecaptcha.render(badgeElement[0],
        {
          'sitekey': siteKey,
          'badge': 'inline',
          'size': 'invisible'
        });

      jQuery(this).off('click.contactus');
      jQuery(this).on('click.contactus', function (event)
      {
        let verificationAction = jQuery(this).attr('data-verification-action');
        let form = jQuery(this).parents('form').first();
        let verificationTokenElement = jQuery(jQuery(this).attr('data-verification-token-element-selector'));

        event.preventDefault();

        grecaptcha.ready(function ()
        {
          grecaptcha.execute(clientId, { action: verificationAction })
            .then(function (token)
            {
              verificationTokenElement.val(token);
              form.on('submit', Page.PostPartialContent);
              form.trigger('submit');
            });
        });
      });
    });
  }
})(jQuery);