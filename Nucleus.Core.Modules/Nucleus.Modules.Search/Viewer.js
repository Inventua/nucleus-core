jQuery(document).ready(function ()
{
	var suggestionsTimeout = -1;

	/* do search on ENTER */
	jQuery('.search-term').on('keydown', function (event)
	{
		if (event.keyCode === 13)
		{
			return _doSearch(this);
		}
	});

	/* the search-suggestions div isn't rendered if the user has disabled search suggestions by setting the max count to zero */
	if (jQuery('.search-suggestions').length !== 0)
	{
		/* Close suggestions when mouse is clicked elsewhere */
		jQuery(document).on('click', '', function ()
		{
			jQuery('.suggestions-result').removeClass('show');
		});

		/* get updated search suggestions */
		jQuery('.search-term').on('input', function (event)
		{
			/* wait half a second before submitting the request, so that if the user is typing, we don't send multiple unwanted requests */
			if (suggestionsTimeout !== -1)
			{
				window.clearTimeout(suggestionsTimeout);
			}

			suggestionsTimeout = window.setTimeout(function (form) { form.submit(); }, 500, jQuery(this).parents('form').first());
			return false;
		});
	}

	jQuery(Page).on("ready", function (event, data)
	{
		if (typeof(data.target) !== 'undefined' && data.target.hasClass('search-suggestions'))
		{
			/*  If suggestions list overflows the right-hand side of the page, move it back to align to the right of the page */
			data.target.css('margin-left', '');
			var overflowpx = jQuery(window).width() - (data.target.offset().left + data.target.outerWidth(true));

			if (overflowpx < 0)
			{
				data.target.css('margin-left', overflowpx);
			}

			data.target.collapse('show');

			data.target.find('.suggestions-result li').on('click', function (event)
			{
				var textbox = jQuery(this).parents('form').find('.search-term');
				textbox.val(jQuery(this).attr('title'));
				_doSearch(this);
			});
		}
	});

	_doSearch = function (element)
	{
		/* if there is a pending call to get search suggestions, cancel it */
		if (suggestionsTimeout !== -1)
		{
			window.clearTimeout(suggestionsTimeout);
		}

    var form = jQuery(element).parents('form');	

    if (typeof form.attr('data-resultsurl') !== 'undefined' && form.attr('data-resultsurl') !== '')
    {
      window.location = form.attr('data-resultsurl') + '?search=' + form.find('.search-term').val();
      return false;
    }

    return true;
	}
});
