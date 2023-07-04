import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Jwt } from 'src/DTO/Jwt';
import { MemberDTO, MemberRoleDTO } from 'src/DTO/MemberDTO/MemberDTO';
import { Group } from 'src/models/Group';

@Injectable({
    providedIn: 'root',
})
export class GroupService {
    constructor(private http: HttpClient) {}

    postGroup(group: Group) {
        return this.http.post<number>('http://localhost:5038/group/', group);
    }

    updateGroup(group: Group) {
        return this.http.put('http://localhost:5038/group/', group);
    }

    deleteGroup(group: Group) {
        return this.http.post('http://localhost:5038/group/remove', group)
    }

    enterGroup(memberData : MemberDTO) {
        return this.http.post('http://localhost:5038/group/enter', memberData);
    }

    quitGroup(memberData : MemberDTO) {
        return this.http.post('http://localhost:5038/group/exit', memberData);
    }

    removeMember(memberData : MemberDTO) {
        return this.http.post('http://localhost:5038/group/remove-member', memberData);
    }

    promoteMember(memberRoleData : MemberRoleDTO) {
        return this.http.post('http://localhost:5038/group/remove-member', memberRoleData);
    }

    listGroups(jwt: Jwt) {
        return this.http.post<Group[]>('http://localhost:5038/group/list', jwt);
    }
}