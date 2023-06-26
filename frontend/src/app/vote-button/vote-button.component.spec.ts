import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VoteButtonComponent } from './vote-button.component';

describe('VoteButtonComponent', () => {
    let component: VoteButtonComponent;
    let fixture: ComponentFixture<VoteButtonComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            declarations: [VoteButtonComponent],
        });
        fixture = TestBed.createComponent(VoteButtonComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
