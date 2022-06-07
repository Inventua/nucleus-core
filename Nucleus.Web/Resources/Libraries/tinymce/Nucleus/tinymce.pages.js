tinymce.util.Tools.resolve("tinymce.PluginManager").add("pages", function (editor, url)
{
	var buildInsertPageModal = function (targetSelector)
	{
		var url = document.baseURI + 'User/PageSelector/Index';
		jQuery.ajax({
			url: url,
			method: 'GET',
			success: function (data, status, request)
			{
				jQuery(targetSelector).html(data);
			}
		});

		var form = jQuery('.nucleus-pageselectorform');
		form.attr('data-target', targetSelector);
		form.attr('action', url);
		form.attr('method', 'POST');
	};

	var openDialog = function ()
	{
		editor.windowManager.open({
			title: "Insert Page Link",
			size: "medium",
			body: {
				type: "panel",
				items: [
					{
						type: "htmlpanel",
						html: '<form class="nucleus-pageselectorform"><div class="nucleus-flex-fields nucleus-pageselector"></div></form>'
					}]
			},
			buttons: [
				{
					type: "submit", name: "save", text: "Insert Page Link", primary: true
				},
				{
					type: "cancel", name: "cancel", text: "Cancel"
				}
			],
			initialData: {},
			onSubmit: function (api)
			{
				var pageSrc = jQuery('.nucleus-pageselector li > a.selected').attr('data-linkurl');
				var pageText = editor.selection.getContent({ format: 'text' }); // editor.getRangeText();
				if (pageText === '')
				{
					pageText = jQuery('.nucleus-pageselector li > a.selected').html();
				}

				editor.insertContent('<a href="' + pageSrc + '">' + pageText + '</a>');

				api.close();
			}
		})

		buildInsertPageModal('.nucleus-pageselector');
	};

	/* Add a button that opens a window */
	editor.ui.registry.addButton('pages', {
		//text: 'Insert Page Link',
		icon: 'embed-page',
		onAction: function ()
		{
			/* Open window */
			openDialog();
		}
	});

	return {
		getMetadata: function ()
		{
			return {
				name: 'Insert Page Link'
			};
		}
	};
});

