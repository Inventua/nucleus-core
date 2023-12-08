jQuery(function ()
{
	jQuery('#site-wizard').modal('show');
	window.setTimeout(TestSiteStarted, 4000);
});

function TestSiteStarted()
{
	jQuery.ajax({
    url: document.baseURI,
    async: true,
		method: 'GET',
		success: function (data, status, request)
		{
			jQuery('.wizard-restarting').removeClass('show');
			jQuery('.wizard-restarted').addClass('show');
		},
		error: function (request, status, message)
		{
			window.setTimeout(TestSiteStarted, 2000);
		}
	})
}
