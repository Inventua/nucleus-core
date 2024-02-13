(function ($)
{
	jQuery.fn.Recaptcha = function (conf)
	{
		// Config for plugin
		var config = jQuery.extend({
			form: null,
			verificationTokenElement: null,
			siteKey: null,
			action: null
		}, conf);
		
		// For every element passed to the plug-in
		return this.each(function (index, value)
		{
			//config.form.off('submit');

			jQuery(this).on('click', function (event)
			{
				if (config.form === null) return;
				if (config.verificationTokenElement === null) return;
				if (config.siteKey === null) return;
				if (config.action === null) return;

				event.preventDefault();

				grecaptcha.ready(function ()
				{
					grecaptcha.execute(config.siteKey, { action: config.action }).then(function (token)
					{
						config.verificationTokenElement.val(token);
						config.form.on('submit', Page.PostPartialContent);
						config.form.trigger('submit');
					});
				});
				

			});
		});
	}
})(jQuery);