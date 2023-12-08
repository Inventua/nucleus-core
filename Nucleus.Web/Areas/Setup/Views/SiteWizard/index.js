var _progressHandle = -1;
jQuery(function ()
{  
  //_setIntegratedSecurityFields = function ()
  //{
  //  var fields = jQuery('.user-pwd-identity');
  //  if (jQuery('#DatabaseUseIntegratedSecurity').is(':checked') || jQuery('#DatabaseProvider').val() === 'Sqlite')
  //  {
  //    fields.removeClass('show');
  //  }
  //  else
  //  {
  //    fields.addClass('show');
  //  }
  //  //  .css('visibility', jQuery('#DatabaseUseIntegratedSecurity').is(':checked') ? 'hidden' : 'visible');
  //}

  //_setSqliteFields();
  //jQuery('#DatabaseProvider').on('change', function (event)
  //{
  //  _setSqliteFields();
  //});

  //_setIntegratedSecurityFields();
  //jQuery(document).on('change', '#DatabaseUseIntegratedSecurity', function (event)
  //{
  //  _setIntegratedSecurityFields();
  //});

  jQuery(Page).on("progress", function ()
  {
    jQuery('.wizard-show-progress').addClass('show');
  });
  jQuery(Page).on("ready", function ()
  {
    jQuery('.wizard-show-progress').removeClass('show');
  });

  jQuery('.site-template-list-group-item').on('click', function (event)
  {
    jQuery('#site-template-select').val(jQuery(this).attr('data-select'));

    jQuery('.site-template-list-group-item').removeClass('active');
    jQuery(this).addClass('active');
  });

  jQuery('#site-template-select').on('change', function (event)
  {
    jQuery('.site-template-list-group-item').removeClass('active');
    jQuery('.site-template-list-group-item[data-select="' + jQuery(this).val() + '"]').addClass('active');
  });
  
  
  jQuery('button[data-bs-toggle="tab"][data-bs-target="#site-install"]').on('shown.bs.tab', function (event)
  {
    // refresh the extensions display if the user navigates from the file systems tab to the extensions tab
    if (jQuery(event.relatedTarget).attr('data-bs-target') === "#site-filesystem")
    {
      jQuery('#refresh-extensions').trigger('click');
    }
  });
  
  jQuery('button[data-bs-toggle="tab"][data-bs-target="#user-settings"]').on('shown.bs.tab', function (event)
  {
    jQuery('#wizard-button-next').removeClass('show');
    jQuery('#wizard-button-finish').addClass('show');
  });

  jQuery('button[data-bs-toggle="tab"]:not([data-bs-target="#user-settings"])').on('shown.bs.tab', function (event)
  {
    jQuery('#wizard-button-next').addClass('show');
    jQuery('#wizard-button-finish').removeClass('show');
  });

  jQuery('button[data-bs-toggle="tab"][data-bs-target="#site-preflight"]').on('shown.bs.tab', function (event)
  {
    jQuery('#wizard-button-back').removeClass('show');
  });

  jQuery('button[data-bs-toggle="tab"]:not([data-bs-target="#site-preflight"])').on('shown.bs.tab', function (event)
  {
    jQuery('#wizard-button-back').addClass('show');
  });

  jQuery('#site-wizard').modal('show');
  jQuery('#wizard-button-back, #wizard-button-next').on('click', navigateTab);

  jQuery('form').on("error", function ()
  {
    if (_progressHandle !== -1)
    {
      window.clearTimeout(_progressHandle);
    }
    jQuery('.wizard-progress').removeClass('show');
  });

  jQuery('#wizard-button-finish').on('click', function ()
  {
    _progressHandle = window.setTimeout(function ()
    {
      jQuery('.wizard-progress').addClass('show');
    }, 2000);
  });
  Page.EnableEnhancedToolTips(true);
});

function navigateTab(e)
{
  e.preventDefault();

  var clickedButton = jQuery(e.currentTarget);
  var currentTab = jQuery('.nav-item').has('.active');
  var newTab;

  if (clickedButton.is('#wizard-button-back'))
  {
    newTab = currentTab.prev();
  }
  else
  {
    newTab = currentTab.next();
  }

  newTab.find('.nav-link').tab('show');
}