$(document).ready(function () {
    InputKeyPress();
    $('#btn_register').click(handleRegistration);


    // When the modal is closed (using Bootstrap's modal events)
    $('#modal-resident').on('hidden.bs.modal', function () {
        $('#form-resident')[0].reset();
    });


    //METHOD TO ENSURE NUMBER ONLY
    function InputKeyPress() {
        const form = $('#form-resident');
        const inputs = form.find('input');

        inputs.each(function () {
            if (this.id === 'rPhone') {
                $('#rPhone').on('keypress', function (e) {

                    var key = e.keyCode || e.which;

                    if (key < 48 || key > 57) {
                        e.preventDefault();
                    }
                })
            }
            else if (this.id === 'rBlock') {
                $('#rBlock').on('keypress', function (e) {

                    var key = e.keyCode || e.which;

                    if (key < 48 || key > 57) {
                        e.preventDefault();
                    }
                })
            }
            
        });
    }

    //METHOD TO ENSURE ALL FIELDS ARE VALID
    // METHOD TO ENSURE ALL FIELDS ARE VALID
    function validateCurrentTab() {
        const currentTabElement = $('#form-resident');
        const inputs = currentTabElement.find('input, select');
        let isValid = true;

        // Use .each() for jQuery collections
        inputs.each(function () {
            if (!this.checkValidity()) {
                isValid = false;
                $(this).addClass('is-invalid');
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        return isValid;
    }

    //GET ADDRESS
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

    //METHOD INSERT
    function handleRegistration(e) {
        e.preventDefault();

        if (!validateCurrentTab()) {
            toastr.error('Please fill out all required fields correctly.');
            return;
        }

        var formData = new FormData();
        var addressesData = [];

        // Gather resident data
        const residentData = {
            Lname: $('#Lname').val(),
            Fname: $('#Fname').val(),
            Mname: $('#Mname').val(),
        };
        // Add resident data to FormData
        for (const key in residentData) {
            formData.append(key, residentData[key]);
        }

        // Gather address data
        $('.row_address').each(function (index, element) {
            var id = "select-street" + (index + 1);
            var block = $(element).find('#rBlock').val();
            var lot = $(element).find('#rLot').val();
            var street = $(element).find('#' + id).val();
            var date = $(element).find('#Date_Residency').val();

            var addressData = { Block: block, Lot: lot, Street_Name: street, Date_Residency: date };
            addressesData.push(addressData);
        });

        // Add addresses as a JSON string
        formData.append('addresses', JSON.stringify(addressesData));


         //AJAX Request
        $.ajax({
            type: 'POST',
            url: '/AdminAccount/RegisterResident',
            data: formData,
            processData: false, // Prevent jQuery from converting FormData to a string
            contentType: false, // Set the Content-Type header to multipart/form-data
            success: function (response) {
                toastr.success(response.message);
                $('#form-resident')[0].reset();
                $('#modal-resident').modal('hide');
                GetRequestAddress();
                console.log(response);
            },
            error: function (xhr) {
                toastr.error(xhr.responseText);
                console.error(xhr.responseText);
            }
        });
    }


    // Remove 'is-invalid' class on input
    $('input, select').on('input', function () {
        $(this).removeClass('is-invalid');
        $(this).closest('form').removeClass('was-validated');
    });
});


/*

function validateCurrentTab() {
        const currentTabElement = document.querySelectorAll('.tab')[currentTab];
        const form = document.querySelector('form');
        const inputs = currentTabElement.querySelectorAll('input, select');

        let isValid = true;

        inputs.forEach(input => {
            if (!input.checkValidity()) {
                isValid = false;
                input.classList.add('was-validated');
            } else {
                input.classList.remove('was-validated');
            }

            if (input.id === 'cpass' && !ConfirmPassword()) {
                isValid = false;
            }
        });

        form.classList.toggle('is-invalid', !isValid);
        form.classList.toggle('was-validated', !isValid);
        return isValid;
    }
*/