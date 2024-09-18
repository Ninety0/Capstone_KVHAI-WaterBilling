$(document).ready(function () {

    //GLOBAL VARIABLE
    let address_id = 0;

    //CALL FUNCTION
    InputKeyPress();

    //EVENT LISTENERS
    $('#select-street').editableSelect();
    $('input, select').on('input change', handleInputChange);
    $(document).on('click', '#btn-cancel', CancelSubmit);

    $(document).on('blur', '#select-street', function () {
        $(this).removeClass('is-invalid');
        $(this).closest('.form-check').find('.invalid-feedback').css('display', 'none');

        $('#input-name').val('');

        GetName();
    });

    $('#btn-read').click(submitReading);


    function GetName() {
        var blk = $('#input-block').val();
        var lot = $('#input-lot').val();
        var st = $('#select-street').val();

        if (blk && lot && st) {
            var formData = $('#form-reading').serialize();
            $.ajax({
                type: 'GET',
                url: '/WaterWorks/GetName',
                data: formData,
                success: function (response) {
                    var result = response[0];
                    address_id = result.address_ID;
                    alert(address_id);
                    $('#input-name').val(result.name);
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                }
            });
        }
        //else {
        //    toastr.error("Please fill out all required fields correctly");
        //}


    }

    function validateTab() {
        var inputElements = $('.water-reading').find('input, select');
        var isValid = true;

        inputElements.each(function () {
            console.log(this.id);
            const input = $(this);

            var feedback = input.closest('.form-check').find('.invalid-feedback');
            if (!this.checkValidity()) {
                input.addClass('is-invalid');
                //input.siblings('.invalid-feedback').css('display', 'block');
                feedback.css('display', 'block');
                feedback.css('font-size', '1rem');
                isValid = false;
            } else {
                input.removeClass('is-invalid');
                //input.siblings('.invalid-feedback').css('display', 'none');
                feedback.css('display', 'none');
            }
        });

        return isValid;
    }

    function submitReading() {
        // Call validateTab and check if the form is valid
        if (!validateTab()) {
            toastr.error("Please complete all required fields.");
        } else {
            if (!$('#input-name').val()) //if value is empty it will be falsy so add (!) operator
            {
                toastr.error("Please complete all required fields.");
                return;
            }
            var formData = {
                Address_ID: address_id,
                Consumption: $('#input-reading').val()
            };

            $.ajax({
                type: 'POST',
                url: '/WaterWorks/SubmitReading',
                data: formData,
                success: function (response) {
                    //var result = $(response).find('.information-section').html();
                    toastr.success(response);
                    $('#form-reading')[0].reset();
                    //$('.information-section').html(result);
                    $('#input-name').val('');
                },
                error: function (xhr, status, err_m) {
                    toastr.error(xhr.responseText);
                }
            });
            // You can add the form submission logic here, e.g., AJAX call or form.submit()
        }
    }

    // Function to handle validation removal and alert check
    function handleInputChange() {
        $(this).removeClass('is-invalid');
        $(this).closest('.form-check').find('.invalid-feedback').css('display', 'none');
    }

    function InputKeyPress() {
        const inputs = $('#form-reading input');

        inputs.each(function () {
            if (this.id === 'input-block' || this.id === 'input-lot' || this.id === 'input-reading') {
                $(this).on('keypress', function (e) {
                    var key = e.keyCode || e.which;

                    if (key < 48 || key > 57) {
                        e.preventDefault();
                    }
                });
            }
        });
    }

    function CancelSubmit()
    {
        $('#form-reading')[0].reset();
        $('#input-name').val('');
        address_id = 0;
        $('.is-invalid').removeClass('is-invalid');
        $('.invalid-feedback').css('display', 'none');
    }

});
