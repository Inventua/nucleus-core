tinymce.util.Tools.resolve("tinymce.PluginManager").add("images", function (editor, url)
{
	var buildInsertImageModal = function (targetSelector)
	{
		var url = document.baseURI + 'User/FileSelector/Index?pattern=(.gif)|(.png)|(.jpg)|(.jpeg)|(.bmp)|(.webp)&showSelectAnother=false&applicationAbsoluteUrl=false&noFilesMessage=(no image files)&showImagePreview=true';
		jQuery.ajax({
      url: url,
      async: true,
			method: 'GET',
			success: function (data, status, request)
			{
				jQuery(targetSelector).html(data);
			}
		});

		var form = jQuery('.nucleus-fileselectorform');
		form.attr('data-target', targetSelector);
		form.attr('action', url);
		form.attr('method', 'POST');
	};

	var openDialog = function ()
	{
		editor.windowManager.open({
			title: "Insert Image",
			size: "medium",
			body: {
				type: "panel",
				items: [
					{
						type: 'htmlpanel',
						html: '<form class="nucleus-fileselectorform"><div class="nucleus-flex-fields nucleus-fileselector"></div></form>'
          },
          {
            type: 'input',
            name: 'caption', 
            inputMode: 'text',
            label: 'Caption', 
            placeholder: 'enter a caption for your image',
            maximized: false 
          }
        ]
			},
			buttons: [
				{
					type: "submit", name: "save", text: "Insert Image", primary: true
				},
				{
					type: "cancel", name: "cancel", text: "Cancel"
				}
			],
			initialData: {},
			onSubmit: function (api)
			{
        var imgSrc = jQuery('.file-link').html();
        var innerContent = '<img alt="' + api.getData().caption + '" src="' + imgSrc + '"/>';
        var content = '';
        if (api.getData().caption !== '')
        {
          content = '<figure>' + innerContent + '<figcaption>' + api.getData().caption + '</figcaption></figure>';
        }
        else
        {
          content = innerContent;
        }
				editor.insertContent(content);
				api.close();
			}
		})

		buildInsertImageModal('.nucleus-fileselector');
	};

	/* Add a button that opens a window */
	editor.ui.registry.addButton('images', {
		tooltip: 'Insert Image',
		icon: 'image',
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
				name: 'Insert Image'
			};
		}
	};
});
