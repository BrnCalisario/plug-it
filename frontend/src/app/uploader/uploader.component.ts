import { HttpClient } from '@angular/common/http';
import { Component, EventEmitter, OnInit, Output, Input } from '@angular/core';

@Component({
    selector: 'app-uploader',
    templateUrl: './uploader.component.html',
    styleUrls: ['./uploader.component.css'],
})
export class UploaderComponent implements OnInit {
    @Output() public onUploadFinished = new EventEmitter<any>();

    @Input() public value: FormData | undefined = new FormData();
    @Input() public title: string = '';
    @Input() public imgUrl: string = '';

    constructor() {}

    ngOnInit(): void {}

    uploadFile = (files: any) => {
        if (files.length == 0) {
            return;
        }

        let fileToUpload = <File>files[0];

        this.value = new FormData();
        this.value.append('file', fileToUpload, fileToUpload.name);
        this.imgUrl = URL.createObjectURL(fileToUpload);

        this.onUploadFinished.emit(this.value);
    };

    getImgSrc() {
        if (this.imgUrl !== '') return this.imgUrl;

        return '../assets/image/avatar-placeholder.png';
    }
}
