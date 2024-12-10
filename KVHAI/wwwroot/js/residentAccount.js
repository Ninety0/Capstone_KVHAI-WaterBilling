$(document).ready(function () {
    GetRequestAddress();

    //SIGNALR SETUP
    const registerConnection = setupSignalRConnection("/staff/register-address", "Register Address Hub");

    registerConnection.on("ReceivedAddressNotificationToAdmin", function () {
        alert("may request");
        GetRequestAddress();
    });

    function GetRequestAddress() {
        $.ajax({
            type: "GET",
            url: "/ResidentAddress/GetRequestAddress",
            success: function (response) {
                var result = $(response).find('#res-tableData').html();
                $('#res-tableData').html(result);
            },
            error: function (xhr) {
                toastr.error(xhr.responseText);
                console.log(xhr.responseText);
            }
        });
    };
    //END SIGNAL R SETUP

    //ON LOAD CALL
    $('#select-street1').editableSelect();

    //event listerner
    $(document).on('click', '.tableContents', function () {
        var btn = $(this).find('.details-section');
        btn.fadeToggle("fast");
    });

    $(document).on('change', '#res-search', function () {
        respagination();
    })

    $(document).on('click', '.respagination', function (event) {
        event.preventDefault();
        //const page = parseInt($(this).text(), 10);
        var respage = parseInt($(this).data('respagination'), 10);
        console.log(respage);
        respagination(respage);
    });

    /*IMAGE GET METHOD*/
    $(document).on('click', '.load-image', function () {
        //var addressID = $(this).siblings('#res_id').val();
        var addressID = $(this).data('id');

        if (addressID) { // Check if residentId has a value
            $('#staticBackdrop').data('address-id', addressID); // Store the residentId in a data attribute of the modal

            // Show preloader before triggering the modal to open
            // Trigger the modal to open
            showPreloader();
            $('#staticBackdrop').modal('show');
        } else {
            alert('No resident ID found.');
        }
    });
    /*END IMAGE METHOD*/

    // Add this new event listener
    $('#staticBackdrop').on('shown.bs.modal', function (e) { // changed to 'shown.bs.modal'
        var modal = $(this);
        var addressID = modal.data('address-id');

        if (addressID) {
            $.ajax({
                type: 'GET',
                url: '/ResidentAddress/GetImageBase64',
                //url: '@Url.Action("GetImageBase64", "AdminAccount")',
                data: { id: addressID },
                success: function (result) {
                    if (result.success) {
                        $('#modalImage').attr('src', 'data:image/jpeg;base64,' + result.imageBase64);
                        $('#modalImage').show();
                    } else {
                        alert('Failed to load image.');
                    }
                    //hidePreloader(); // Hide preloader after the image is successfully loaded
                },
                error: function () {
                    alert('An error occurred while loading the image.');
                    hidePreloader(); // Hide preloader if there's an error
                }
            });
        } else {
            hidePreloader(); // Hide preloader if there's no residentId
            alert('No resident ID available.');
        }
    });

    // Hide preloader when the modal is hidden
    $('#staticBackdrop').on('hidden.bs.modal', function () {
        // Reset modal content when it's closed
        $('#modalImage').attr('src', '').hide();
        $('#imagePreloader').show();
    });


    function hidePreloader() {
        const loader = $('#loader');
        setTimeout(function () {
            $('#loader').addClass('d-none');
        }, 2000);

    }

    function showPreloader() {
        const loader = $('#loader');
        loader.removeClass('d-none');
        setTimeout(function () {
            $('#loader').addClass('d-none');
        }, 2000);
    }

    function respagination(i = 1) {//Yung i is default pero pwedeng ibang letter ilagay dyan [i] lang nilalagay ko
        var _search = $('#res-search').val();
        var _category = $('#res-category').val();

        var array = {
            search: _search,
            page_index: i
            
        };

 
        $.ajax({
            url: window.location.origin + '/ResidentAddress/ResidentPagination',
            type: "POST",
            data: array,
            success: function (response) {
                var result = $(response).find("#res-tableData").html();
                console.log(result);
                $('#res-tableData').html(result)

                var btnEdit = $('.btn-res-edit');
                var btnDelete = $('.btn-res-delete');

                if (_isActive) {
                    $('.btn-res-edit').addClass('disabled');
                    $('.btn-res-delete').addClass('disabled');

                }
                else {
                    $('.btn-res-edit').removeClass('disabled');
                    $('.btn-res-delete').removeClass('disabled');
                }
            },
            error: function (xhr, status, error_m) {
                toastr.error(xhr.responseText);
            }
        });
    }
});